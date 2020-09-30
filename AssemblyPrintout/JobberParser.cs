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

        static char[] Numbers = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

        /// <summary>
        /// Pulls all .BKO files from remote source and parses them into usable data.
        /// </summary>
        /// <returns>Dicionary<string, Customer>(CustomerCode, CustomerModel)</returns>
        public static Dictionary<string, Customer> ParseBackOrders()
        {
            Dictionary<string, Customer> Customers = new Dictionary<string, Customer>();
            foreach (string item0 in FileManager.GetDirFiles(Paths.SalesDir, "BKO"))
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
                                Customers[CustomerCode].Invoices.Add(InvoiceNumber, new Invoice(InvoiceNumber));
                            Customers[CustomerCode].Invoices[InvoiceNumber].BackorderedItems.Add(new BackorderedItem(InvoiceNumber, data));
                        }
                        else
                        {
                            Util.Log($"Backorder file corruption in file {InvoiceNumber}.bko. Please fix the file before running again.", new ErrorType[] { ErrorType.CSharpError, ErrorType.JobberError });
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
            Dictionary<int, string> FileData = new Dictionary<int, string>();
            // Get all invoice files from jobber pc and put into Dict<Invoice#,Path> to be used later
            var InvoiceFiles = FileManager.GetDirFiles($@"{Paths.SalesDir}\{DateTime.Now.Year}").ToList();
            foreach (string f in InvoiceFiles)
            {
                if (f.Length > 7 && int.TryParse(f.Split('\\').Last().Substring(0, f.Split('\\').Last().Length - 7), out int InvoiceNum) && !FileData.ContainsKey(InvoiceNum))
                    FileData.Add(InvoiceNum, f);
            }
            foreach (KeyValuePair<string, Customer> C in Customers)
            {
                foreach (KeyValuePair<int, Invoice> I in C.Value.Invoices)
                {
                    if (FileData.ContainsKey(I.Key))
                    {
                        try
                        {
                            using (StreamReader sr = new StreamReader(FileData[I.Key]))
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
                C.Value.Invoices = C.Value.Invoices ?? new Dictionary<int, Invoice>();
                //// .ToDictionary(x => x.Key, x => x.Value);
                foreach (var x in C.Value.Invoices.OrderBy(x => x.Value.OrderDateRaw).OrderBy(x => x.Value.OrderDate))
                {
                    if (!C.Value.Invoices.ContainsKey(x.Key)) C.Value.Invoices.Add(x.Key, x.Value);
                }
            }
            return Customers;
        }

        public static void DumpData(Dictionary<string, Customer> Customers)
        {
            ProductAllocationModel ProductAllocation = new ProductAllocationModel();
            ProductAllocation.AddProducts(Util.Products);
            Dictionary<int, BackorderedItem> AllItems = new Dictionary<int, BackorderedItem>();
            List<Invoice> AllInvoices = new List<Invoice>();
            double SummedValue = 0;
            AllInvoices = Customer.ExtractInvoices(Customers.Values);
            ProductAllocation.TotalProductBackorders(AllInvoices);
            AllInvoices = ProductAllocation.AllocateInvoices(AllInvoices);

            using (StreamWriter sw = new StreamWriter(Paths.ExportBackorderValData))
            {
                Util.SourceOfflineWarning(sw);
                Util.JobberOfflineWarning(sw);
                sw.WriteLine("Item | On Order | Allocatable | Value Of Allocatable");
                foreach (var item in ProductAllocation.AllocatedItems.Values.OrderBy(x => x.ProductNumber))
                {
                    double DollarVal = (item.Allocated * Util.ProductDictionary[item.ProductNumber].ListPrice);
                    SummedValue += DollarVal;
                    sw.WriteLine("{0}| {1} | {2} |{3} | {4}",
                        item.ProductNumber,
                        Util.PadL(8 - item.QuantityOnBackOrder.ToString().Length) + item.QuantityOnBackOrder.ToString(),
                        Util.PadL(11 - item.Allocated.ToString().Length) + item.Allocated.ToString(),
                        Util.PadL(13 - DollarVal.ToString("C").Length) + DollarVal.ToString("C"),
                        Util.PadL(16 - SummedValue.ToString("C").Length) + SummedValue.ToString("C"));
                }
            }
            using (StreamWriter sw = new StreamWriter(Paths.ExportBackorderVal)) sw.WriteLine(SummedValue);
            DumpActual(ProductAllocation);
        }

        public static void DumpData(Dictionary<string, Customer> Customers, int ProductNumber)
        {
            List<BackorderedItemExportHelper> ExportData = new List<BackorderedItemExportHelper>();
            foreach (var C in Customers.Values)
            {
                foreach (var I in C.Invoices.Values.Where(x => x.BackorderedItems.Select(xx => xx.ItemNumber).Contains(ProductNumber )))
                {
                    IEnumerable<BackorderedItem> Items;
                    if ((Items = I.BackorderedItems.Where(x => x.ItemNumber == ProductNumber)).Any())
                        foreach (var B in Items)
                        {
                            ExportData.Add(new BackorderedItemExportHelper(C, I, B));
                        }
                }
            }

            if (ExportData.Any())
                using (StreamWriter sw = new StreamWriter(Paths.ExportGenericData))
                {
                    sw.WriteLine($"\t{ProductNumber} was found in {ExportData.Count} backorders.");
                    sw.WriteLine();
                    ExportData.OrderByDescending(x => x.I.OrderDate).ToList().ForEach(item => { sw.WriteLine(item.ToString()); });
                    sw.WriteLine($"{ExportData.Sum(x => x.B.QuantityOnBackOrder)}");
                }
            Process.Start(new ProcessStartInfo(Paths.ExportGenericData));
        }

        /// <summary>
        /// Parses and sorts all backorders to be exported to a production report.
        /// </summary>
        /// <param name="Customers"></param>
        /// <param name="DateLimit"></param>
        public static void DumpData(Dictionary<string, Customer> Customers, DateTime DateLimit)
        {
            Dictionary<int, ProductModel> PopulatedProductDict = new Dictionary<int, ProductModel>();
            Util.Products.ForEach(item =>
            {
                PopulatedProductDict.Add(item.Number, item.PopulateProduct(Util.ProductDictionary, Util.KitDictionary));
            });
            ProductAllocationModel ProductAllocation = new ProductAllocationModel();
            ProductAllocationModel ProductAllocationByDate = new ProductAllocationModel();
            ProductAllocation.AddProducts(Util.Products);
            ProductAllocationByDate.AddProducts(Util.Products);
            Dictionary<int, BackorderedItem> AllItems = new Dictionary<int, BackorderedItem>();
            List<Invoice> AllInvoices = new List<Invoice>();
            List<Invoice> FilteredInvoices = new List<Invoice>();
            foreach (var Customer in Customers.Values) AllInvoices.AddRange(Customer.Invoices.Values);
            ProductAllocation.TotalProductBackorders(AllInvoices);
            ProductAllocation.AllocateInvoices(AllInvoices);
            FilteredInvoices.AddRange(AllInvoices.Where(x => x.OrderDate <= DateLimit));
            ProductAllocationByDate.TotalProductBackorders(FilteredInvoices);
            ProductAllocationByDate.AllocateInvoices(FilteredInvoices);
            var PartsUnderOnePercent = Util.Parts.Where(x => ((double)(x.QuantityOnHand - x.QuantityInProducts) / x.YearsUse) <= 0.01).Select(x => x.PartNumber).ToList();
            Dictionary<int, PartModel> LowParts = new Dictionary<int, PartModel>();
            List<int> CodeOrder = new List<int>();
            foreach (string CodeString in Read.GenericRead($@"{Paths.SourceDir}\NEEDPROD.DAT")) { if (int.TryParse(CodeString, out int CodeInt)) CodeOrder.Add(CodeInt); }
            var ActualOnHand = ProductAllocation.AllocatedItems.ToDictionary(x => x.Value.ProductNumber, x => x.Value.ActualOnHand);
            var TotalOnOrder = ProductAllocation.AllocatedItems.ToDictionary(x => x.Value.ProductNumber, x => x.Value.QuantityOnBackOrder);
            var ProductsByCode = ProductAllocationByDate.AllocatedItems.Where(x => x.Value.QuantityOnBackOrder > 0 && x.Value.QuantityOnBackOrder > ActualOnHand[x.Value.ProductNumber]).GroupBy(x => (int)((x.Value.Product.ProductCode - 1000) * 0.1)).ToDictionary(x => x.Key);
            using (StreamWriter sw = new StreamWriter(Paths.ExportProductNeeded))
            {
                Util.SourceOfflineWarning(sw);
                Util.JobberOfflineWarning(sw);
                sw.WriteLine($"Showing backorders older than {DateLimit.ToShortDateString()}");
                sw.WriteLine();
                CodeOrder.ForEach(CodeX =>
                {
                    IGrouping<int, KeyValuePair<int, ProductAllocationItem>> Code;
                    if (ProductsByCode.ContainsKey(CodeX) && (Code = ProductsByCode?[CodeX]) != null)
                    {
                        sw.WriteLine(" Code ┌───────────────────────────────────┬──────────┬───────────────┬─────────┬──────────────┐");
                        sw.WriteLine($" {Util.PadL(4, (Code.Key * 10 + 1000).ToString())} │ Product                           │ On Order │ Actual On Hand│ Produce |Total On Order│");

                        Code.OrderBy(x => x.Key).ToList().ForEach(Prod =>
                        {
                            sw.WriteLine($"      │ {Prod.Value.ProductNumber} - {Util.PadR(25, Prod.Value.Product.Description.Trim())} │{Util.PadL(9, Prod.Value.QuantityOnBackOrder.ToString())} │{Util.PadL(14, ActualOnHand[Prod.Value.ProductNumber].ToString())} │{Util.PadL(8, (Prod.Value.QuantityOnBackOrder - ActualOnHand[Prod.Value.ProductNumber]).ToString())} │{Util.PadL(13, TotalOnOrder[Prod.Value.ProductNumber].ToString())} │");
                            var LocalLowParts = new List<int>(Prod.Value.Product.RequiredParts.Select(x => x.Key).Where(x => PartsUnderOnePercent.Contains(x)));
                            LocalLowParts.ForEach(item =>
                            {
                                if (!LowParts.ContainsKey(item)) LowParts.Add(item, Util.PartDictionary[item]);
                                sw.WriteLine($"      │ {(item != LocalLowParts.Last() ? "├" : "└")}──────────> **Low Part**  {Util.PartDictionary[item].PartTypePrefix}{item} ╞══════════╪═══════════════╪═════════╪══════════════╡");
                            });

                        });
                        sw.WriteLine("      └───────────────────────────────────┴──────────┴───────────────┴─────────┴──────────────┘");
                    }
                });
                for (int i = 0; i < 5; i++) sw.WriteLine();
                if (LowParts.Any()) sw.WriteLine("Compiled list of low parts from above product list.");
                List<int> MachinesOne = new List<int>(LowParts.Values.GroupBy(x => x.MachineNumberOne).Where(x => x.Key > 0).Select(x => x.Key));
                sw.WriteLine();
                if (MachinesOne.Any()) sw.WriteLine("Machines by 1st Cycle:");
                MachinesOne.ForEach(item =>
                {
                    IEnumerable<PartModel> TempLows;
                    if ((TempLows = LowParts.Values.Where(x => x.MachineNumberOne == item)).Any())
                    {
                        sw.WriteLine("┌───────┬─────────────────────────────┐");
                        sw.WriteLine($"│Machine│#{item}{Util.PadL(28 - item.ToString().Length)}│");
                        sw.WriteLine("└───────┼─────────────────────────────┤");
                        TempLows.ToList().ForEach(item1 =>
                        {
                            sw.WriteLine($"        │{item1.PartTypePrefix}{item1.PartNumber} - {item1.PartDescription}│");
                        });
                        sw.WriteLine("        └─────────────────────────────┘");
                    }
                });
                sw.WriteLine();
            }
            Process.Start("notepad.exe", Paths.ExportProductNeeded);
            DumpActual(ProductAllocation);
        }

        public static void DumpData(Dictionary<string, Customer> Customers, string ReportType, string CustomerCode, bool PublicFields = false)
        {
            int TotalItems = 0, ShownBackorders = 0;
            Dictionary<int, Invoice> InvoiceDict = new Dictionary<int, Invoice>();
            ProductAllocationModel ProductAllocation = new ProductAllocationModel();
            ProductAllocation.AddProducts(Util.Products);
            List<Invoice> AllInvoices = new List<Invoice>();
            foreach (var Customer in Customers.Values) AllInvoices.AddRange(Customer.Invoices.Values);
            ProductAllocation.TotalProductBackorders(AllInvoices);
            AllInvoices = ProductAllocation.AllocateInvoices(AllInvoices);
            using (StreamWriter sw = new StreamWriter(Paths.ExportBackorder))
            {
                Util.SourceOfflineWarning(sw);
                Util.JobberOfflineWarning(sw);
                sw.WriteLine();
                AllInvoices.ForEach(item =>
                {
                    if (!InvoiceDict.ContainsKey(item.InvoiceNumber)) InvoiceDict.Add(item.InvoiceNumber, item);
                    else Console.WriteLine($"Duplicate {item.InvoiceNumber}");
                });

                //var InvoiceDict = AllInvoices.ToDictionary(x => x.InvoiceNumber, x => x);
                foreach (KeyValuePair<string, Customer> C in (string.IsNullOrEmpty(CustomerCode) ? Customers : Customers.Where(x => x.Key.ToUpper() == CustomerCode.ToUpper())))
                {
                    var TempInvoiceList = AllInvoices.Where(x => C.Value.Invoices.Select(xx => xx.Key).Contains(x.InvoiceNumber)).ToList();
                    Dictionary<int, Invoice> ValidInvoices = new Dictionary<int, Invoice>();
                    (ReportType == "1" ? TempInvoiceList.Where(x => x.BackorderedItems.Where(xx => xx.QuantityAllocatable > 0 || ProductAllocation.AllocatedItems[xx.ItemNumber + 90000].ActualOnHand < 0).Any()) :
                        ReportType == "2" ? TempInvoiceList.Where(x => x.BackorderedItems.Where(xx => xx.QuantityAllocatable < xx.QuantityOnBackOrder).Any()) :
                        TempInvoiceList).ToList().ForEach(item => { if (!ValidInvoices.ContainsKey(item.InvoiceNumber)) ValidInvoices.Add(item.InvoiceNumber, item); });

                    if (ValidInvoices.Any())
                    {
                        sw.WriteLine("Customer: {0} => {1} Backordered invoice{2}", C.Value.CustomerCode, ValidInvoices.Count, (ValidInvoices.Count > 1 ? "s" : ""));
                        C.Value.CustomerInfo.ForEach(item => { sw.WriteLine("\t{0}", item); });
                        int BackorderIndex = 1;
                        Dictionary<int, BackorderedItem> thisInvoiceItems;
                        foreach (KeyValuePair<int, Invoice> I in ValidInvoices.OrderBy(x => x.Value.OrderDate))
                        {
                            thisInvoiceItems = new Dictionary<int, BackorderedItem>();
                            InvoiceDict[I.Key].BackorderedItems.ForEach(item =>
                            {
                                if (thisInvoiceItems.ContainsKey(item.ItemNumber))
                                {
                                    thisInvoiceItems[item.ItemNumber].QuantityOnBackOrder += item.QuantityOnBackOrder;
                                    thisInvoiceItems[item.ItemNumber].QuantityAllocatable = item.QuantityOnBackOrder;
                                }
                                else thisInvoiceItems.Add(item.ItemNumber, item);
                            });
                            if (!thisInvoiceItems.Any()) continue;
                            int Count = 0;
                            sw.WriteLine();
                            sw.WriteLine("#{6}| Invoice #:{0} PO#:{1} - {2} | Discount {3}% | {4} Backordered Item{5}",
                                I.Key,
                                I.Value.PurchaseOrderNumber,
                                I.Value.OrderDate != null ? I.Value.OrderDate.ToString() : I.Value.OrderDateRaw,
                                I.Value.Discount,
                                I.Value.BackorderedItems.Count,
                                (I.Value.BackorderedItems.Count > 1 ? "s" : ""),
                                (BackorderIndex + Util.PadL()).Substring(0, 4));
                            sw.WriteLine("Item |Number|Desc                    |Ordered|{0}", (PublicFields ? "  OnHand| AOnHand| Fill Qty|" : string.Empty));
                            BackorderIndex++;
                            foreach (BackorderedItem B in I.Value.BackorderedItems.Where(xx => (ReportType == "1" ? xx.QuantityAllocatable > 0 || ProductAllocation.AllocatedItems[xx.ItemNumber + 90000].ActualOnHand < 0 : ReportType == "2" ? xx.QuantityAllocatable < xx.QuantityOnBackOrder : ProductAllocation.AllocatedItems.Keys.Contains(xx.ItemNumber + 90000))))
                            {
                                sw.WriteLine("{0}  | {1}|{2}|{3}|{4}{5}{6}",
                                    (Count + Util.PadL()).Substring(0, 3),
                                    (90000 + B.ItemNumber).ToString(),
                                    (B.ItemName + Util.PadL()).Substring(0, 24),
                                    Util.PadL(7 - B.QuantityOnBackOrder.ToString().Length) + B.QuantityOnBackOrder.ToString(),
                                    (PublicFields ? Util.PadL(8 - ProductAllocation.AllocatedItems[ProductAllocationModel.ProductNumber(B.ItemNumber)].QuantityOnHand.ToString().Length) + ProductAllocation.AllocatedItems[ProductAllocationModel.ProductNumber(B.ItemNumber)].QuantityOnHand.ToString() + "|" : string.Empty),
                                    (PublicFields ? Util.PadL(8 - ProductAllocation.AllocatedItems[ProductAllocationModel.ProductNumber(B.ItemNumber)].ActualOnHand.ToString().Length) + ProductAllocation.AllocatedItems[ProductAllocationModel.ProductNumber(B.ItemNumber)].ActualOnHand.ToString() + "|" : string.Empty),
                                    (PublicFields ? Util.PadL(9 - ProductAllocation.AllocatedItems[ProductAllocationModel.ProductNumber(B.ItemNumber)].AllocatedOnOrder(B.InvoiceNumber).ToString().Length) + ProductAllocation.AllocatedItems[ProductAllocationModel.ProductNumber(B.ItemNumber)].AllocatedOnOrder(B.InvoiceNumber).ToString() + "|" : string.Empty));
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
                using (StreamWriter sw = new StreamWriter(Paths.ExportActualOnHand))
                    ProductAllocation.AllocatedItems.OrderBy(x => x.Key).ToList().ForEach(Prods => { sw.WriteLine("{0}{1}", Prods.Key, Prods.Value.ActualOnHand); });

            using (StreamWriter sw = new StreamWriter(Paths.ExportTotalBackOrdered))
            {
                sw.WriteLine(ProductAllocation.TotalBackorderedItems);
                sw.WriteLine(ProductAllocation.TotalValueOfBackOrders * 0.54);
            }
        }

    }
}
