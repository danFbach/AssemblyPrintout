using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using static AssemblyPrintout.Datatypes;

namespace AssemblyPrintout
{
    static class JobberParser
    {
        static string Space => "                                                   ";
        static string DashS => "───────────────────────────────────────────────────";
        public static string Pad(int length = -1, bool Dash = false) => length >= 0 ? (!Dash ? Space : DashS).Substring(0, length) : (!Dash ? Space : DashS);

        static char[] Numbers = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

        /// <summary>
        /// Pulls all .BKO files from remote source and parses them into usable data.
        /// </summary>
        /// <returns>Dicionary<string, Customer>(CustomerCode, CustomerModel)</returns>
        public static Dictionary<string, Customer> ParseBackOrders()
        {
            Dictionary<string, Customer> Customers = new Dictionary<string, Customer>();
            foreach (string item0 in FileManager.GetDirFiles(SalesDir, "BKO"))
            {
                var Filename = new FileInfo(item0);
                if (Filename.Name.ToLower() == "total.bko") continue;

                var NumTemp = Filename.Name.Replace(".BKO", "");
                string FinalNumber = string.Empty;
                foreach (char n in NumTemp) if (Numbers.Contains(n)) FinalNumber += n; else break;

                if (!int.TryParse(FinalNumber, out int InvoiceNumber)) continue;

                using (StreamReader sr = new StreamReader(item0))
                {
                    string line;
                    while (!string.IsNullOrEmpty(line = sr.ReadLine()))
                    {
                        if (string.IsNullOrEmpty(line.Trim())) continue;
                        string[] data = line.Split(',');
                        if (data.Length == 5)
                        {
                            string CustomerId = data[3].Trim();
                            string CustomerCode = data[4].Trim();
                            if (!Customers.ContainsKey(CustomerCode)) Customers.Add(CustomerCode, new Customer(CustomerId, CustomerCode));
                            if (!Customers[CustomerCode].Invoices.ContainsKey(InvoiceNumber))
                                Customers[CustomerCode].Invoices.Add(InvoiceNumber, new InvoiceModel(InvoiceNumber));
                            Customers[CustomerCode].Invoices[InvoiceNumber].BackorderedItems.Add(new BackorderedItem(InvoiceNumber, data));
                        }
                        else
                        {
                            Utilities.Log($"Backorder file corruption in file {InvoiceNumber}.bko. Please fix the file before running again.", new ErrorType[] { ErrorType.CSharpError, ErrorType.JobberError });
                            Environment.Exit(-1);
                        }
                    }
                }
            };
            return Customers;
        }

        /// <summary>
        /// An ugly turd of a function that parses customers and invoices and organizes them into models.
        /// </summary>
        /// <param name="Customers">List of customerss obtained by previous function.</param>
        /// <returns>Dicionary<string, Customer>(CustomerCode, CustomerModel)</returns>
        public static Dictionary<string, Customer> GetInvoiceData(Dictionary<string, Customer> Customers)
        {
            Dictionary<string, string> FileData = new Dictionary<string, string>();
            // Get all invoice files from jobber pc and put into Dict<Invoice#,Path> to be used later
            foreach (string f in FileManager.GetDirFiles($@"{SalesDir}\{DateTime.Now.Year}"))
            {
                if (f.Length > 7)
                {
                    string InvoiceNumber = f.Substring(0, f.Length - 7).Split('\\').Last();
                    if (!FileData.ContainsKey(InvoiceNumber)) FileData.Add(InvoiceNumber, f);
                }
            }
            foreach (KeyValuePair<string, Customer> C in Customers)
            {
                foreach (KeyValuePair<int, InvoiceModel> I in C.Value.Invoices)
                {
                    if (FileData.ContainsKey(I.Key.ToString()))
                    {
                        try
                        {
                            using (StreamReader sr = new StreamReader(FileData[I.Key.ToString()]))
                            {
                                string Line;
                                int count = 0;
                                bool FoundDate = false;
                                while ((Line = sr.ReadLine()) != null)
                                {
                                    count++;
                                    if (Line.Contains(I.Key.ToString()) && !FoundDate)
                                    {
                                        var DateRaw = Line.Trim().Split(' ').Last().Split('-');
                                        if (DateRaw.Length == 3 && DateTime.TryParse((I.Value.OrderDateRaw = string.Format($@"{DateRaw[0]}/{DateRaw[1]}/20{DateRaw[2]}")), out DateTime thisDate))
                                            I.Value.OrderDate = thisDate;

                                        FoundDate = true;
                                        continue;
                                    }

                                    if (count <= 17)
                                    {
                                        if (Line.Contains("PAGE")) continue;
                                        if (string.IsNullOrEmpty(Line.Trim()) || C.Value.CustomerInfo.Contains(Line.Trim())) continue;
                                        if (Line.Contains(C.Value.CustomerCode))
                                        {
                                            I.Value.PurchaseOrderNumber = Line.Split(' ').First();
                                            continue;
                                        }
                                        C.Value.CustomerInfo.Add(Line.Trim());
                                    }
                                    else if (Line.ToLower().Contains("discount") && Line.ToLower().Contains("%") && !Line.ToLower().Contains("freight") && !Line.ToLower().Contains("discounted"))
                                    {
                                        I.Value.Discount = decimal.TryParse(Line.Split('%').First(), out decimal Discount) ? Discount : -1;
                                    }
                                }
                            }
                        }
                        catch
                        {
                            continue;
                        }
                    }
                }
                C.Value.Invoices = C.Value.Invoices.OrderBy(x => x.Value.OrderDateRaw).OrderBy(x => x.Value.OrderDate).ToDictionary(x => x.Key, x => x.Value);
            }
            return Customers;
        }

        public static void DumpData(Dictionary<string, Customer> Customers)
        {
            ProductAllocationModel ProductAllocation = new ProductAllocationModel();
            ProductAllocation.AddProducts(Utilities.Products);
            Dictionary<int, BackorderedItem> AllItems = new Dictionary<int, BackorderedItem>();
            List<InvoiceModel> AllInvoices = new List<InvoiceModel>();
            double SummedValue = 0;
            AllInvoices = Customer.ExtractInvoices(Customers.Values);
            ProductAllocation.TotalProductBackorders(AllInvoices);
            AllInvoices = ProductAllocation.AllocateInvoices(AllInvoices);

            using (StreamWriter sw = new StreamWriter($@"{SourceDir}\TEMPDATA\BackOrderValData.txt"))
            {
                sw.WriteLine("Item | On Order | Allocatable | Value Of Allocatable");
                DevDataWarning(sw);
                foreach (var item in ProductAllocation.AllocatedItems.Values.OrderBy(x => x.ProductNumber))
                {
                    double DollarVal = (item.Allocated * Utilities.ProductDictionary[item.ProductNumber].ListPrice);
                    SummedValue += DollarVal;
                    sw.WriteLine("{0}| {1} | {2} |{3} | {4}",
                        item.ProductNumber,
                        Pad(8 - item.QuantityOnBackOrder.ToString().Length) + item.QuantityOnBackOrder.ToString(),
                        Pad(11 - item.Allocated.ToString().Length) + item.Allocated.ToString(),
                        Pad(13 - DollarVal.ToString("C").Length) + DollarVal.ToString("C"),
                        Pad(16 - SummedValue.ToString("C").Length) + SummedValue.ToString("C"));
                }
            }
            using (StreamWriter sw = new StreamWriter($@"{SourceDir}\TEMPDATA\BackOrderVal.txt")) sw.WriteLine(SummedValue);
            DumpActual(ProductAllocation);
        }

        /// <summary>
        /// Parses and sorts all backorders to be exported to a production report.
        /// </summary>
        /// <param name="Customers"></param>
        /// <param name="DateLimit"></param>
        public static void DumpData(Dictionary<string, Customer> Customers, DateTime DateLimit)
        {
            Dictionary<int, ProductModel> PopulatedProductDict = new Dictionary<int, ProductModel>();
            Utilities.Products.ForEach(item =>
            {
                PopulatedProductDict.Add(item.Number, item.PopulateProduct(Utilities.ProductDictionary, Utilities.KitDictionary));
            });
            ProductAllocationModel ProductAllocation = new ProductAllocationModel();
            ProductAllocationModel ProductAllocationByDate = new ProductAllocationModel();
            ProductAllocation.AddProducts(Utilities.Products);
            ProductAllocationByDate.AddProducts(Utilities.Products);
            Dictionary<int, BackorderedItem> AllItems = new Dictionary<int, BackorderedItem>();
            List<InvoiceModel> AllInvoices = new List<InvoiceModel>();
            List<InvoiceModel> FilteredInvoices = new List<InvoiceModel>();
            foreach (var Customer in Customers.Values) AllInvoices.AddRange(Customer.Invoices.Values);
            ProductAllocation.TotalProductBackorders(AllInvoices);
            ProductAllocation.AllocateInvoices(AllInvoices);
            FilteredInvoices.AddRange(AllInvoices.Where(x => x.OrderDate <= DateLimit));
            ProductAllocationByDate.TotalProductBackorders(FilteredInvoices);
            ProductAllocationByDate.AllocateInvoices(FilteredInvoices);
            var PartsUnderOnePercent = Utilities.Parts.Where(x => ((double)(x.QuantityOnHand - x.QuantityInProducts) / x.YearsUse) <= 0.01).Select(x => x.PartNumber).ToList();
            Dictionary<int, PartModel> LowParts = new Dictionary<int, PartModel>();
            List<int> CodeOrder = new List<int>();
            foreach (string CodeString in Read.GenericRead($@"{SourceDir}\NEEDPROD.DAT")) { if (int.TryParse(CodeString, out int CodeInt)) CodeOrder.Add(CodeInt); }
            var ActualOnHand = ProductAllocation.AllocatedItems.ToDictionary(x => x.Value.ProductNumber, x => x.Value.ActualOnHand);
            var TotalOnOrder = ProductAllocation.AllocatedItems.ToDictionary(x => x.Value.ProductNumber, x => x.Value.QuantityOnBackOrder);
            var ProductsByCode = ProductAllocationByDate.AllocatedItems.Where(x => x.Value.QuantityOnBackOrder > 0 && x.Value.QuantityOnBackOrder > ActualOnHand[x.Value.ProductNumber]).GroupBy(x => (int)((x.Value.Product.ProductCode - 1000) * 0.1)).ToDictionary(x => x.Key);
            using (StreamWriter swe = new StreamWriter(@"C:\inven\ProductsNeeded.txt"))
            {
                DevDataWarning(swe);
                swe.WriteLine($"Showing backorders older than {DateLimit.ToShortDateString()}");
                swe.WriteLine();
                CodeOrder.ForEach(CodeX =>
                {
                    IGrouping<int, KeyValuePair<int, ProductAllocationItem>> Code;
                    if (ProductsByCode.ContainsKey(CodeX) && (Code = ProductsByCode?[CodeX]) != null)
                    {
                        swe.WriteLine(" Code ┌───────────────────────────────────┬──────────┬───────────────┬──────────────┐");
                        swe.WriteLine($" {Pad(4 - (Code.Key * 10 + 1000).ToString().Length)}{Code.Key * 10 + 1000} │ Product                           │ On Order │ Actual On Hand│Total On Order│");

                        Code.OrderBy(x => x.Key).ToList().ForEach(Prod =>
                        {
                            swe.WriteLine($"      │ {Prod.Value.ProductNumber} - {Prod.Value.Product.Description.Trim()} {Pad(25 - Prod.Value.Product.Description.Trim().Length)}│{Pad(8 - Prod.Value.QuantityOnBackOrder.ToString().Length)} {Prod.Value.QuantityOnBackOrder} │{Pad(14 - ActualOnHand[Prod.Value.ProductNumber].ToString().Length)} {ActualOnHand[Prod.Value.ProductNumber]}│{Pad(13 - TotalOnOrder[Prod.Value.ProductNumber].ToString().Length)} {TotalOnOrder[Prod.Value.ProductNumber]}│");
                            Prod.Value.Product.RequiredParts.Select(x => x.Key).ToList().ForEach(item =>
                            {
                                if (PartsUnderOnePercent.Contains(item))
                                {
                                    swe.WriteLine($"      ├─> **Low Part**  {Utilities.PartDictionary[item].PartTypePrefix}{item}            │          │               │              │");
                                    if (!LowParts.ContainsKey(item)) LowParts.Add(item, Utilities.PartDictionary[item]);
                                }
                            });
                        });
                        swe.WriteLine("      └───────────────────────────────────┴──────────┴───────────────┴──────────────┘");
                    }
                });
                for (int i = 0; i < 5; i++) swe.WriteLine();
                if (LowParts.Any()) swe.WriteLine("Compiled list of low parts from above product list.");
                List<int> MachinesOne = new List<int>(LowParts.Values.GroupBy(x => x.MachineNumberOne).Where(x => x.Key > 0).Select(x => x.Key));
                swe.WriteLine();
                if (MachinesOne.Any()) swe.WriteLine("Machines by 1st Cycle:");
                MachinesOne.ForEach(item =>
                {
                    IEnumerable<PartModel> TempLows;
                    if ((TempLows = LowParts.Values.Where(x => x.MachineNumberOne == item)).Any())
                    {
                        swe.WriteLine("┌───────┬─────────────────────────────┐");
                        swe.WriteLine($"│Machine│#{item}{Pad(28 - item.ToString().Length)}│");
                        swe.WriteLine("└───────┼─────────────────────────────┤");
                        TempLows.ToList().ForEach(item1 =>
                        {
                            swe.WriteLine($"        │{item1.PartTypePrefix}{item1.PartNumber} - {item1.PartDescription}│");
                        });
                        swe.WriteLine("        └─────────────────────────────┘");
                    }
                });
                swe.WriteLine();
            }
            Process.Start("notepad.exe", @"C:\inven\ProductsNeeded.txt");
            DumpActual(ProductAllocation);
        }

        public static void DumpData(Dictionary<string, Customer> Customers, string ReportType, string CustomerCode, bool PublicFields = false)
        {
            int TotalItems = 0, ShownBackorders = 0;
            string Export = @"C:\Inven\BackOrderExport.txt";
            ProductAllocationModel ProductAllocation = new ProductAllocationModel();
            ProductAllocation.AddProducts(Utilities.Products);
            List<InvoiceModel> AllInvoices = new List<InvoiceModel>();
            foreach (var Customer in Customers.Values) AllInvoices.AddRange(Customer.Invoices.Values);
            ProductAllocation.TotalProductBackorders(AllInvoices);
            AllInvoices = ProductAllocation.AllocateInvoices(AllInvoices);
            using (StreamWriter sw = new StreamWriter(Export))
            {
                DevDataWarning(sw);
                sw.WriteLine();
                var InvoiceDict = AllInvoices.ToDictionary(x => x.InvoiceNumber, x => x);
                foreach (KeyValuePair<string, Customer> C in (string.IsNullOrEmpty(CustomerCode) ? Customers : Customers.Where(x => x.Key.ToUpper() == CustomerCode.ToUpper())))
                {
                    var TempInvoiceList = AllInvoices.Where(x => C.Value.Invoices.Select(xx => xx.Key).Contains(x.InvoiceNumber)).ToList();

                    Dictionary<int, InvoiceModel> ValidInvoices = ReportType == "1" ?
                        TempInvoiceList.Where(x => x.BackorderedItems.Where(xx => xx.QuantityAllocatable > 0).Any()).ToDictionary(x => x.InvoiceNumber, x => x) :
                        ReportType == "2" ? TempInvoiceList.Where(x => x.BackorderedItems.Where(xx => xx.QuantityAllocatable < xx.QuantityOnOrder).Any()).ToDictionary(x => x.InvoiceNumber, x => x) :
                        TempInvoiceList.ToDictionary(x => x.InvoiceNumber, x => x);
                    if (ValidInvoices.Any())
                    {
                        sw.WriteLine("Customer: {0} => {1} Backordered invoice{2}", C.Value.CustomerCode, ValidInvoices.Count, (ValidInvoices.Count > 1 ? "s" : ""));
                        C.Value.CustomerInfo.ForEach(item => { sw.WriteLine("\t{0}", item); });
                        int BackorderIndex = 1;
                        Dictionary<int, BackorderedItem> thisInvoiceItems;
                        foreach (KeyValuePair<int, InvoiceModel> I in ValidInvoices.OrderBy(x => x.Value.OrderDate))
                        {
                            thisInvoiceItems = new Dictionary<int, BackorderedItem>();
                            InvoiceDict[I.Key].BackorderedItems.ForEach(item =>
                            {
                                if (thisInvoiceItems.ContainsKey(item.ItemNumber))
                                {
                                    thisInvoiceItems[item.ItemNumber].QuantityOnOrder += item.QuantityOnOrder;
                                    thisInvoiceItems[item.ItemNumber].QuantityAllocatable = item.QuantityOnOrder;
                                }
                                else thisInvoiceItems.Add(item.ItemNumber, item);
                            });
                            int Count = 0;
                            sw.WriteLine();
                            sw.WriteLine(
                                "#{6}| Invoice #:{0} PO#:{1} - {2} | Discount {3}% | {4} Backordered Item{5}",
                                I.Key,
                                I.Value.PurchaseOrderNumber,
                                I.Value.OrderDate != null ? I.Value.OrderDate.ToString() : I.Value.OrderDateRaw,
                                I.Value.Discount,
                                I.Value.BackorderedItems.Count,
                                (I.Value.BackorderedItems.Count > 1 ? "s" : ""),
                                (BackorderIndex + Pad()).Substring(0, 4));
                            sw.WriteLine("Item |Number|Desc                    |Ordered|{0}", (PublicFields ? "  OnHand| AOnHand| Fill Qty|" : string.Empty));
                            BackorderIndex++;
                            foreach (BackorderedItem B in I.Value.BackorderedItems.Where(xx => (ReportType == "1" ? xx.QuantityAllocatable > 0 : ReportType == "2" ? xx.QuantityAllocatable < xx.QuantityOnOrder : ProductAllocation.AllocatedItems.Keys.Contains(xx.ItemNumber + 90000))))
                            {
                                sw.WriteLine("{0}  | {1}|{2}|{3}|{4}{5}{6}",
                                    (Count + Pad()).Substring(0, 3),
                                    (90000 + B.ItemNumber).ToString(),
                                    (B.ItemName + Pad()).Substring(0, 24),
                                    Pad(7 - B.QuantityOnOrder.ToString().Length) + B.QuantityOnOrder.ToString(),
                                    (PublicFields ? Pad(8 - ProductAllocation.AllocatedItems[ProductAllocationModel.ProductNumber(B.ItemNumber)].QuantityOnHand.ToString().Length) + ProductAllocation.AllocatedItems[ProductAllocationModel.ProductNumber(B.ItemNumber)].QuantityOnHand.ToString() + "|" : string.Empty),
                                    (PublicFields ? Pad(8 - ProductAllocation.AllocatedItems[ProductAllocationModel.ProductNumber(B.ItemNumber)].ActualOnHand.ToString().Length) + ProductAllocation.AllocatedItems[ProductAllocationModel.ProductNumber(B.ItemNumber)].ActualOnHand.ToString() + "|" : string.Empty),
                                    (PublicFields ? Pad(8 - ProductAllocation.AllocatedItems[ProductAllocationModel.ProductNumber(B.ItemNumber)].AllocatedOnOrder(B.InvoiceNumber).ToString().Length) + ProductAllocation.AllocatedItems[ProductAllocationModel.ProductNumber(B.ItemNumber)].AllocatedOnOrder(B.InvoiceNumber).ToString() + "|" : string.Empty));
                                Count++;
                            }
                            TotalItems += Count;
                        }
                        ShownBackorders += BackorderIndex;
                        sw.WriteLine();
                        sw.WriteLine();
                    }
                }
                if (TotalItems == 0 && ShownBackorders == 0)
                {
                    sw.WriteLine("Sorry! There aren't currently any backorders that can be filled with the given criteria.");
                    sw.WriteLine("Submit a request with no customer specified to see a full list of backorders that can be filled,");
                    sw.WriteLine("or run backorder report [9] which will show all current and open backorders.");
                }
            }
            Process.Start("notepad.exe", Export);
            DumpActual(ProductAllocation);
        }

        /// <summary>
        /// Exports data on each product calculating what is actually on the shelf and available.
        /// </summary>
        /// <param name="ProductAllocation">Model must be prepopulated with data.</param>
        public static void DumpActual(ProductAllocationModel ProductAllocation)
        {
            if (ProductAllocation.AllocatedItems.Any())
                using (StreamWriter sw = new StreamWriter($@"{SourceDir}\TEMPDATA\ActualOnHand.tmp"))
                    ProductAllocation.AllocatedItems.OrderBy(x => x.Key).ToList().ForEach(Prods => { sw.WriteLine("{0}{1}", Prods.Key, Prods.Value.ActualOnHand); });

            using (StreamWriter sw = new StreamWriter(PathVars.TotalBackOrdered))
            {
                sw.WriteLine(ProductAllocation.TotalBackorderedItems);
                sw.WriteLine(ProductAllocation.TotalValueOfBackOrders * 0.54);
            }
        }

        /// <summary>
        /// Posts to active write file a warning stating that development data is being used, not live data.
        /// </summary>
        /// <param name="sw">An active StreamWriter</param>
        public static void DevDataWarning(StreamWriter sw) { if (!Utilities.JobberIsOnline || Utilities.InDevMode()) sw.WriteLine("** WARNING - JOBBER PC IS OFF, DATA BEING USED IS OUTDATED FILLER DATA USED FOR TESTING PURPOSES **"); }
    }
}
