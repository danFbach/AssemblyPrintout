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

        /// <summary>
        /// Pulls all .BKO files from remote source and parses them into usable data.
        /// </summary>
        /// <returns>Dicionary<string, Customer>(CustomerCode, CustomerModel)</returns>

        public static void DumpData()
        {
            ProductAllocationModel ProductAllocation = new ProductAllocationModel();
            ProductAllocation.AddProducts(Util.Products);
            Dictionary<int, BackorderedItem> AllItems = new Dictionary<int, BackorderedItem>();
            List<Invoice> AllInvoices = new List<Invoice>();
            double SummedValue = 0, SummedValueB = 0;
            var Customers = Util.CustomerDictionary;
            AllInvoices = Customer.ExtractInvoices(Customers.Values);
            ProductAllocation.TotalProductBackorders(AllInvoices);
            ProductAllocation.AllocateInvoices(AllInvoices);

            using (StreamWriter sw = new StreamWriter(Paths.ExportBackorderValData))
            {

                Util.SourceOfflineWarning(sw);
                Util.JobberOfflineWarning(sw);
                sw.WriteLine("Item | On Order | Allocatable |  Allc Value  |Total Allc Value |BackLessAllcVal|BackLessAllcValTotal");
                foreach (var item in ProductAllocation.AllocatedItems.Values.OrderBy(x => x.ProductNumber))
                {
                    double DollarValAllocatable = (item.Allocated * (Util.ProductDictionary[item.ProductNumber].ListPrice * 0.5));
                    double DollarValBackordered = ((item.QuantityOnBackOrder - item.Allocated) * (Util.ProductDictionary[item.ProductNumber].ListPrice * 0.5));
                    SummedValue += DollarValAllocatable;
                    SummedValueB += DollarValBackordered;
                    sw.WriteLine("{0}| {1} | {2} |{3} |{4} | {5} | {6}",
                        item.ProductNumber,
                        Util.PadL(8 - item.QuantityOnBackOrder.ToString().Length) + item.QuantityOnBackOrder.ToString(),
                        Util.PadL(11 - item.Allocated.ToString().Length) + item.Allocated.ToString(),
                        Util.PadL(13 - DollarValAllocatable.ToString("C").Length) + DollarValAllocatable.ToString("C"),
                        Util.PadL(16 - SummedValue.ToString("C").Length) + SummedValue.ToString("C"),
                        Util.PadL(13 - DollarValBackordered.ToString("C").Length) + DollarValBackordered.ToString("C"),
                        Util.PadL(16 - SummedValueB.ToString("C").Length) + SummedValueB.ToString("C")
                        );

                }
            }
            using (StreamWriter sw = new StreamWriter(Paths.ExportBackorderVal)) sw.WriteLine(SummedValue);
            DumpActual(ProductAllocation);
        }

        public static void DumpData(int ProductNumber)
        {
            List<BackorderedItemExportHelper> ExportData = new List<BackorderedItemExportHelper>();
            var Customers = Util.CustomerDictionary;
            foreach (var C in Customers.Values)
            {
                foreach (var I in C.Invoices.Values.Where(x => x.BackorderedItems.Select(xx => xx.ItemNumber).Contains(ProductNumber)))
                {
                    IEnumerable<BackorderedItem> Items;
                    if ((Items = I.BackorderedItems.Where(x => x.ItemNumber == ProductNumber)).Any())
                        foreach (var B in Items)
                            ExportData.Add(new BackorderedItemExportHelper(C, I, B));
                }
            }

            if (ExportData.Any())
                using (StreamWriter sw = new StreamWriter(Paths.ExportGenericData))
                {
                    sw.WriteLine($"\t{(ProductNumber + 90000)} was found in {ExportData.Count} backorders.");
                    sw.WriteLine();
                    ExportData.OrderByDescending(x => x.I.InvoiceNumber).ToList().ForEach(item => { sw.WriteLine(item.ToString()); });
                    sw.WriteLine($"                                                                             Total:{Util.PadL(8, ExportData.Sum(x => x.B.QuantityOnBackOrder).ToString())}");
                }
            Process.Start(new ProcessStartInfo(Paths.ExportGenericData));
        }

        /// <summary>
        /// Parses and sorts all backorders to be exported to a production report.
        /// </summary>
        /// <param name="Customers"></param>
        /// <param name="DateLimit"></param>
        public static void DumpData(DateTime DateLimit, List<string> CodeData = null)
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
            var Customers = Util.CustomerDictionary;
            foreach (var Customer in Customers.Values) AllInvoices.AddRange(Customer.Invoices.Values);
            ProductAllocation.TotalProductBackorders(AllInvoices);
            ProductAllocation.AllocateInvoices(AllInvoices);
            FilteredInvoices.AddRange(AllInvoices.Where(x => x.OrderDate <= DateLimit));
            ProductAllocationByDate.TotalProductBackorders(FilteredInvoices);
            ProductAllocationByDate.AllocateInvoices(FilteredInvoices);
            var PartsUnderOnePercent = Util.Parts.Where(x => ((double)(x.QuantityOnHand - x.QuantityInProducts) / x.YearsUse) <= 0.01).Select(x => x.PartNumber).ToList();
            Dictionary<int, PartModel> LowParts = new Dictionary<int, PartModel>();
            List<int> CodeOrder = new List<int>();
            if (CodeData == null)
                foreach (string CodeString in Read.GenericRead(Paths.ImportNeedProd))
                {
                    if (int.TryParse(CodeString, out int CodeInt)) CodeOrder.Add(CodeInt);
                }
            else
                for (int i = 1; i < CodeData.Count; i++)
                    if (int.TryParse(CodeData[i], out int Code))
                        CodeOrder.Add((int)((Code - 1000) * 0.1));

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
                if (CodeData == null)
                {
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
            }
            Process.Start("notepad.exe", Paths.ExportProductNeeded);
            DumpActual(ProductAllocation);
        }
        public static void DumpData(DateTime DateLimit)
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
            var Customers = Util.CustomerDictionary;
            foreach (var Customer in Customers.Values) AllInvoices.AddRange(Customer.Invoices.Values);
            ProductAllocation.TotalProductBackorders(AllInvoices);
            ProductAllocation.AllocateInvoices(AllInvoices);
            FilteredInvoices.AddRange(AllInvoices.Where(x => x.OrderDate <= DateLimit));
            ProductAllocationByDate.TotalProductBackorders(FilteredInvoices);
            ProductAllocationByDate.AllocateInvoices(FilteredInvoices);
            var PartsUnderOnePercent = Util.Parts.Where(x => ((double)(x.QuantityOnHand - x.QuantityInProducts) / x.YearsUse) <= 0.01).Select(x => x.PartNumber).ToList();
            Dictionary<int, PartModel> LowParts = new Dictionary<int, PartModel>();
            List<int> CodeOrder = new List<int>();
            foreach (string CodeString in Read.GenericRead(Paths.ImportNeedProd))
            {
                if (int.TryParse(CodeString, out int CodeInt)) CodeOrder.Add(CodeInt);
            }

            var ActualOnHand = ProductAllocation.AllocatedItems.ToDictionary(x => x.Value.ProductNumber, x => x.Value.ActualOnHand);
            var TotalOnOrder = ProductAllocation.AllocatedItems.ToDictionary(x => x.Value.ProductNumber, x => x.Value.QuantityOnBackOrder);
            var ProductsByCode = ProductAllocationByDate.AllocatedItems.Where(x => x.Value.QuantityOnBackOrder > 0 && x.Value.QuantityOnBackOrder > ActualOnHand[x.Value.ProductNumber]).GroupBy(x => (int)((x.Value.Product.ProductCode - 1000) * 0.1)).ToDictionary(x => x.Key);
            using (StreamWriter sw = new StreamWriter(Paths.ExportProductNeeded))
            {
                Util.SourceOfflineWarning(sw);
                Util.JobberOfflineWarning(sw);
                CodeOrder.ForEach(CodeX =>
                {
                    IGrouping<int, KeyValuePair<int, ProductAllocationItem>> Code;
                    if (ProductsByCode.ContainsKey(CodeX) && (Code = ProductsByCode?[CodeX]) != null)
                    {
                        Code.OrderBy(x => x.Key).ToList().ForEach(Prod =>
                        {
                            var LocalLowParts = new List<int>(Prod.Value.Product.RequiredParts.Select(x => x.Key).Where(x => PartsUnderOnePercent.Contains(x)));
                            LocalLowParts.ForEach(item =>
                            {
                                if (!LowParts.ContainsKey(item)) LowParts.Add(item, Util.PartDictionary[item]);
                            });

                        });
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

        public static void DumpData(string ReportType, string CustomerCode, bool PublicFields = false)
        {
            int TotalItems = 0, ShownBackorders = 0;
            Dictionary<int, Invoice> InvoiceDict = new Dictionary<int, Invoice>();
            ProductAllocationModel ProductAllocation = new ProductAllocationModel();
            ProductAllocation.AddProducts(Util.Products);
            List<Invoice> AllInvoices = new List<Invoice>();
            var Customers = Util.CustomerDictionary;
            foreach (var Customer in Customers.Values) AllInvoices.AddRange(Customer.Invoices.Values);
            ProductAllocation.TotalProductBackorders(AllInvoices);
            ProductAllocation.AllocateInvoices(AllInvoices);
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
                        foreach (KeyValuePair<int, Invoice> I in ValidInvoices.OrderBy(x => x.Value.InvoiceNumber))
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
            Process.Start("notepad.exe", Paths.ExportBackorder);
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

            ProductAllocationModel ExportOrderAllocation = new ProductAllocationModel();
            ExportOrderAllocation.AddProducts(Util.Products);
            ProductAllocationModel DomOrderAllocation = new ProductAllocationModel();
            DomOrderAllocation.AddProducts(Util.Products);

            List<Invoice> ExpInvoices = new List<Invoice>();
            List<Invoice> DomInvoices = new List<Invoice>();
            List<Invoice> AllInvoices = new List<Invoice>();
            var Customers = Util.CustomerDictionary;

            foreach (var Customer in Customers) AllInvoices.AddRange(Customer.Value.Invoices.Values);
            ExportOrderAllocation.TotalProductBackorders(AllInvoices);
            DomOrderAllocation.TotalProductBackorders(AllInvoices);

            foreach (var Customer in Customers.Where(x => x.Key.StartsWith("E"))) ExpInvoices.AddRange(Customer.Value.Invoices.Values);
            var ExpCannotShip = ExportOrderAllocation.AllocateInvoices(ExpInvoices);

            foreach (var Customer in Customers.Where(x => !x.Key.StartsWith("E"))) DomInvoices.AddRange(Customer.Value.Invoices.Values);
            var DomCannotShip = DomOrderAllocation.AllocateInvoices(DomInvoices);

            using (StreamWriter sw = new StreamWriter(Paths.ExportTotalBackOrdered))
            {
                sw.WriteLine(ProductAllocation.TotalBackorderedItems);
                sw.WriteLine(ProductAllocation.TotalValueOfBackOrders);
                double ExpBkoVal = 0;
                foreach (KeyValuePair<int, int> item in ExpCannotShip)
                {
                    ExpBkoVal += Util.ProductDictionary.ContainsKey(item.Key + 90000) ? ((Util.ProductDictionary[item.Key + 90000].ListPrice * .5) * item.Value) : 0;
                }
                sw.WriteLine(ExpBkoVal);
                sw.WriteLine(ExportOrderAllocation.TotalValueOfShippable);
                double DomBkoVal = 0;
                foreach (KeyValuePair<int, int> item in DomCannotShip)
                {
                    DomBkoVal += Util.ProductDictionary.ContainsKey(item.Key + 90000) ? ((Util.ProductDictionary[item.Key + 90000].ListPrice * .5) * item.Value) : 0;
                }
                sw.WriteLine(DomBkoVal);
                sw.WriteLine(DomOrderAllocation.TotalValueOfShippable);
            }
        }
    }
}
