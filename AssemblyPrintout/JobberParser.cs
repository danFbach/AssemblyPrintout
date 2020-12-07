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
            Dictionary<int, BackorderedItem> AllItems = new Dictionary<int, BackorderedItem>();
            List<Invoice> AllInvoices = new List<Invoice>();
            double SummedValue = 0, SummedValueB = 0;
            ProductAllocationModel ProductAllocation = new ProductAllocationModel(Customer.ExtractInvoices(Util.CustomerDictionary));

            using (StreamWriter sw = new StreamWriter(Paths.ExportBackorderValData))
            {
                Util.SourceOfflineWarning(sw);
                Util.JobberOfflineWarning(sw);
                sw.WriteLine("Item | On Order | Allocatable |  Allc Value  |Total Allc Value |BackLessAllcVal|BackLessAllcValTotal");
                foreach (var item in ProductAllocation.AllocatedItems.Values.Where(x => x.QuantityOnBackOrder > 0).OrderBy(x => x.ProductNumber))
                {
                    double DollarValAllocatable = (item.Allocated * (Util.ProductDictionary[item.ProductNumber].ListPrice * 0.5));
                    double DollarValBackordered = ((item.QuantityOnBackOrder - item.Allocated) * (Util.ProductDictionary[item.ProductNumber].ListPrice * 0.5));
                    SummedValue += DollarValAllocatable;
                    SummedValueB += DollarValBackordered;
                    sw.WriteLine("{0}| {1} | {2} |{3} |{4} | {5} | {6}",
                        item.ProductNumber,
                        Util.Pad(Side.Left, 8 - item.QuantityOnBackOrder.ToString().Length) + item.QuantityOnBackOrder.ToString(),
                        Util.Pad(Side.Left, 11 - item.Allocated.ToString().Length) + item.Allocated.ToString(),
                        Util.Pad(Side.Left, 13 - DollarValAllocatable.ToString("C").Length) + DollarValAllocatable.ToString("C"),
                        Util.Pad(Side.Left, 16 - SummedValue.ToString("C").Length) + SummedValue.ToString("C"),
                        Util.Pad(Side.Left, 13 - DollarValBackordered.ToString("C").Length) + DollarValBackordered.ToString("C"),
                        Util.Pad(Side.Left, 16 - SummedValueB.ToString("C").Length) + SummedValueB.ToString("C")
                        );
                }
            }
            using (StreamWriter sw = new StreamWriter(Paths.ExportBackorderVal)) sw.WriteLine(SummedValue);
            DumpActual(ProductAllocation);
        }

        public static void DumpData(int ProductNumber)
        {
            List<BackorderedItemExportHelper> ExportData = new List<BackorderedItemExportHelper>();

            foreach (var C in Util.CustomerDictionary.Values)
            {
                foreach (var I in C.Invoices.Values.Where(x => x.BackorderedItems.Select(xx => xx.ItemNumber).Contains(ProductNumber)))
                {
                    IEnumerable<BackorderedItem> Items;
                    if ((Items = I.BackorderedItems.Where(x => x.ItemNumber == ProductNumber)).Any())
                        foreach (var B in Items)
                            ExportData.Add(new BackorderedItemExportHelper(C, I, B));
                }
            }

            List<int> Prioritized = new List<int>();
            var Priority = Util.InvoicePriorities.Where(x => ExportData.Select(x1 => x1.I.InvoiceNumber).Contains(x.Key)).ToDictionary(x => x.Key, x => x.Value);
            Prioritized = Priority.Select(x => x.Key).ToList();

            if (ExportData.Any())
                using (StreamWriter sw = new StreamWriter(Paths.ExportGenericData))
                {
                    sw.WriteLine($"\t{(ProductNumber + 90000)} was found in {ExportData.Count} backorder{(ExportData.Count != 1 ? "s" : "")}.");
                    sw.WriteLine();
                    ExportData.OrderByDescending(x => x.I.OrderDate).ThenByDescending(x => x.I.InvoiceNumber).ToList().ForEach(item =>
                    {
                        var TempPriority = Priority.Where(x => x.Value > item.I.OrderDate).ToDictionary(x => x.Key, x => x.Value);
                        foreach (var TP in TempPriority)
                        {
                            var ExData = ExportData.Find(x => x.I.InvoiceNumber == TP.Key);
                            if (ExData != null)
                                sw.WriteLine(ExData.ToString() + $" * {TP.Value.ToString("MM/dd/yyyy")}");
                            Priority.Remove(TP.Key);
                        }
                        if (!Prioritized.Contains(item.I.InvoiceNumber))
                            sw.WriteLine(item.ToString());
                    });
                    foreach (var TP in Priority.OrderByDescending(x => x.Value))
                    {
                        var ExData = ExportData.Find(x => x.I.InvoiceNumber == TP.Key);
                        if (ExData != null)
                            sw.WriteLine(ExData.ToString() + $" * {TP.Value.ToString("MM/dd/yyyy")}");
                    }
                    sw.WriteLine($"                                                                             Total:{Util.Pad(Side.Left, 8, ExportData.Sum(x => x.B.QuantityOnBackOrder).ToString())}");
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
            List<Invoice> AllInvoices = Customer.ExtractInvoices(Util.CustomerDictionary);

            ProductAllocationModel ProductAllocation = new ProductAllocationModel(AllInvoices);
            ProductAllocationModel ProductAllocationByDate = new ProductAllocationModel(AllInvoices.Where(x => x.OrderDate <= DateLimit).ToList());

            var PartsUnderOnePercent = Util.Parts.Where(x => ((double)(x.QuantityOnHand - x.QuantityInProducts) / x.YearsUse) <= 0.01).Select(x => x.PartNumber).ToList();
            Dictionary<int, PartModel> LowParts = new Dictionary<int, PartModel>();
            List<int> CodeOrder = new List<int>();
            if (CodeData == null)
            {
                foreach (string CodeString in Read.GenericRead(Paths.ImportNeedProd))
                    if (int.TryParse(CodeString, out int CodeInt))
                        CodeOrder.Add(CodeInt);
            }
            else
            {
                for (int i = 1; i < CodeData.Count; i++)
                    if (int.TryParse(CodeData[i], out int Code))
                        CodeOrder.Add((int)((Code - 1000) * 0.1));
            }

            var ActualOnHand = ProductAllocation.AllocatedItems.ToDictionary(x => x.Value.ProductNumber, x => x.Value.ActualOnHand);
            var TotalOnOrder = ProductAllocation.AllocatedItems.ToDictionary(x => x.Value.ProductNumber, x => x.Value.QuantityOnBackOrder);
            var ProductsByCode = ProductAllocationByDate.AllocatedItems.Where(x => x.Value.QuantityOnBackOrder > 0 && x.Value.QuantityOnBackOrder > ActualOnHand[x.Value.ProductNumber]).GroupBy(x => (int)((x.Value.Product.ProductCode - 1000) * 0.1)).ToDictionary(x => x.Key);
            using (StreamWriter sw = new StreamWriter(Paths.ExportProductNeeded))
            {
                Util.SourceOfflineWarning(sw);
                Util.JobberOfflineWarning(sw);
                sw.WriteLine($"Showing backorders older than {DateLimit.ToString("MM/dd/yyyy")}");
                sw.WriteLine();
                CodeOrder.ForEach(CodeX =>
                {
                    IGrouping<int, KeyValuePair<int, ProductAllocationItem>> Code;
                    if (ProductsByCode.ContainsKey(CodeX) && (Code = ProductsByCode?[CodeX]) != null)
                    {
                        sw.WriteLine(" Code ┌───────────────────────────────────┬──────────┬───────────────┬─────────┬──────────────┐");
                        sw.WriteLine($" {Util.Pad(Side.Left, 4, (Code.Key * 10 + 1000).ToString())} │ Product                           │ On Order │ Actual On Hand│ Produce │Total On Order│");

                        Code.OrderBy(x => x.Key).ToList().ForEach(Prod =>
                        {
                            sw.WriteLine($"      │ {Prod.Value.ProductNumber} - {Util.Pad(Side.Right, 25, Prod.Value.Product.Description.Trim())} │{Util.Pad(Side.Left, 9, Prod.Value.QuantityOnBackOrder.ToString())} │{Util.Pad(Side.Left, 14, ActualOnHand[Prod.Value.ProductNumber].ToString())} │{Util.Pad(Side.Left, 8, (Prod.Value.QuantityOnBackOrder - ActualOnHand[Prod.Value.ProductNumber]).ToString())} │{Util.Pad(Side.Left, 13, TotalOnOrder[Prod.Value.ProductNumber].ToString())} │");
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
                            sw.WriteLine($"│Machine│#{item}{Util.Pad(Side.Left, 28 - item.ToString().Length)}│");
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
            List<Invoice> AllInvoices = Customer.ExtractInvoices(Util.CustomerDictionary);

            ProductAllocationModel ProductAllocation = new ProductAllocationModel(AllInvoices);
            ProductAllocationModel ProductAllocationByDate = new ProductAllocationModel(AllInvoices.Where(x => x.OrderDate <= DateLimit).ToList());

            var PartsUnderOnePercent = Util.Parts.Where(x => ((double)(x.QuantityOnHand - x.QuantityInProducts) / x.YearsUse) <= 0.01).Select(x => x.PartNumber).ToList();
            Dictionary<int, PartModel> LowParts = new Dictionary<int, PartModel>();
            List<int> CodeOrder = new List<int>();

            foreach (string CodeString in Read.GenericRead(Paths.ImportNeedProd))
                if (int.TryParse(CodeString, out int CodeInt))
                    CodeOrder.Add(CodeInt);

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
                        sw.WriteLine($"│Machine│#{item}{Util.Pad(Side.Left, 28 - item.ToString().Length)}│");
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
            int TotalItems = 0, ShownBackorders = 0, Cols = 8;
            List<Invoice> AllInvoices = Customer.ExtractInvoices(Util.CustomerDictionary);
            ProductAllocationModel ProductAllocation = new ProductAllocationModel(AllInvoices);
            string ExportFile = ReportType != "5" ? Paths.ExportBackorderHtml : Paths.ExportBackorderCsv;
            using (StreamWriter sw = new StreamWriter(ExportFile))
            {
                if (ReportType != "5")
                {
                    sw.WriteLine("<!DOCTYPE html><html xmlns=\"http://www.w3.org/1999/xhtml\" lang=\"en\" dir=\"ltr\" dark=\"true\">" +
                        "<head>\n<meta content=\"text/html;charset=utf-8\" http-equiv=\"Content-Type\"><meta content=\"utf-8\" http-equiv=\"encoding\">\n" +
                        "<style>* { font-family:\"Courier New\"; font-weight:600; font-size:10pt; letter-spacing: -0.4px; } .editable { width: 100%; display: inline-block; cursor: pointer; } .fill-qty, .fill-qty input { width: 75px; } thead { display: table-row-group; } td, th { border: 1px solid #000;padding: 2px;} td { white-space: nowrap; overflow: hidden; } table {border-collapse: collapse; width:600px; } .right { text-align:right; } .f-right {float:right;} .no-vis { border: none; } .boxes { min-width:75px; } .none { display:none; } .no-print, .no-print * { background:#CCC; } .toggler { width: 15px; } @media print { .no-print, .no-print * { display: none !important; }}</style>\n" +
                        "<script type=\"text/javascript\">\n" +
                        "function span_insert(obj, forceOldVal) {\n" +
                        "obj.outerHTML = '<span class=\"editable\" ondblclick=\"input_insert(this)\">' + (isNaN(obj.value) || obj.value == undefined || forceOldVal ? parseInt(obj.parentElement.dataset.oval) : obj.value)  + '</span>';\n" +
                        "}\n" +
                        "function input_insert(obj) {\n" +
                        "let tempid = Date.now();" +
                        "obj.outerHTML = '<input id=\"' + tempid + '\" type=\"number\" value=\"' + (isNaN(obj.innerHTML) || obj.innerHTML == undefined ? parseInt(obj.parentElement.dataset.oval) : parseInt(obj.innerHTML)) + '\" onkeyup=\"(event.which || event.keyCode) == 13 ? span_insert(this, false) : (event.which || event.keyCode) == 27 ? span_insert(this, true) : null;\" />';\n" +
                        "document.getElementById(tempid).focus();" +
                        "}\n" +
                        "function span_text_insert(obj, forceOldVal) {\n" +
                        "obj.outerHTML = '<span class=\"editable\" ondblclick=\"input_text_insert(this)\">' + (obj.value == undefined || forceOldVal ? obj.parentElement.dataset.oval : obj.value)  + '</span>';\n" +
                        "}\n" +
                        "function input_text_insert(obj) {\n" +
                        "let tempid = Date.now();" +
                        "obj.outerHTML = '<input id=\"' + tempid + '\" type=\"text\" value=\"' + (obj.innerHTML == undefined ? obj.parentElement.dataset.oval : obj.innerHTML == 'Employee' ? '' : obj.innerHTML) + '\" onkeyup=\"(event.which || event.keyCode) == 13 ? span_text_insert(this, false) : (event.which || event.keyCode) == 27 ? span_text_insert(this, true) : null;\" />';\n" +
                        "document.getElementById(tempid).focus();" +
                        "}\n" +
                        "</script>\n" +
                        "</head>\n" +
                        "<body>\n");
                    sw.WriteLine("<table>\n<tbody>");
                }
                //onfocusout =\"span_insert(this)\" 
                //Util.SourceOfflineWarning(sw);
                //Util.JobberOfflineWarning(sw);

                foreach (KeyValuePair<string, Customer> C in (string.IsNullOrEmpty(CustomerCode) ? Util.CustomerDictionary : Util.CustomerDictionary.Where(x => x.Key.ToUpper() == CustomerCode.ToUpper())))
                {
                    var TempInvoiceList = AllInvoices.Where(x => C.Value.Invoices.Select(xx => xx.Key).Contains(x.InvoiceNumber)).ToList();
                    Dictionary<int, Invoice> ValidInvoices = new Dictionary<int, Invoice>();

                    (ReportType == "1" ? TempInvoiceList.Where(x => x.BackorderedItems.Where(xx => xx.QuantityAllocatable > 0 || ProductAllocation.AllocatedItems[xx.ItemNumber + 90000].ActualOnHand < 0).Any()) :
                        ReportType == "2" ? TempInvoiceList.Where(x => x.BackorderedItems.Where(xx => xx.QuantityAllocatable < xx.QuantityOnBackOrder).Any()) :
                        TempInvoiceList).ToList().ForEach(item =>
                        {
                            if (!ValidInvoices.ContainsKey(item.InvoiceNumber))
                                ValidInvoices.Add(item.InvoiceNumber, item);
                        });

                    if (ValidInvoices.Any())
                    {
                        int BackorderIndex = 1;
                        double CanShipSum = 0, TotalSum = 0;
                        if (ReportType == "5")
                        {
                            sw.WriteLine("Customer: {0} => {1} Backordered invoice{2}", C.Value.CustomerCode, ValidInvoices.Count, ValidInvoices.Count > 1 ? "s" : "");
                            sw.WriteLine("\t{0}", C.Value.CustomerInfo.First());
                        }
                        else
                        {
                            sw.WriteLine("<tr><td colspan=\"" + Cols + "\">Customer: {0} => {1} Backordered invoice{2}<br>", C.Value.CustomerCode, ValidInvoices.Count, ValidInvoices.Count > 1 ? "s" : "");
                            sw.WriteLine("{0}<br>", C.Value.CustomerInfo.First());
                            sw.WriteLine("</td></tr>" +
                                "<tr><td colspan=\"" + Cols + "\">To be packed by:<span class=\"editable\" data-oval=\"Employee\" ondblclick=\"input_text_insert(this)\">Employee</span></td></tr>");
                        }
                        if (ReportType == "4")
                        {
                            sw.WriteLine("<tr><td></td><td>Invoice #</td><td>PO# - Order Date</td><td>Discount</td><td># Backordered Item(s)</td><td colspan=\"" + (Cols - 7) + "\">Shippable<span class=\"f-right\"></span></td><td colspan=\"" + (Cols - 6) + "\">Total:<span class=\"f-right\"><span></td></tr>");
                        }

                        foreach (KeyValuePair<int, Invoice> I in ValidInvoices.OrderBy(x => x.Value.OrderDate).ThenBy(x => x.Value.InvoiceNumber))
                        {
                            DateTime? EstimatedShip = null;
                            if (I.Value.OrderDate.HasValue && Util.EDS.ContainsKey(I.Value.OrderDate.Value.Date))
                                EstimatedShip = DateTime.Now.AddDays(Util.InvoicePriorities.ContainsKey(I.Value.InvoiceNumber) ? Util.EDS[Util.InvoicePriorities[I.Value.InvoiceNumber]] : Util.EDS[I.Value.OrderDate.Value.Date] < 0 ? Util.EDS[I.Value.OrderDate.Value.Date] * -1 : 0);

                            int Count = 0;
                            if (ReportType == "4")
                            {
                                double CanShipVal = I.Value.BackorderedItems
                                   .Where(xx => xx.QuantityAllocatable > 0 || ProductAllocation.AllocatedItems[xx.ItemNumber + 90000].ActualOnHand < 0)
                                   .Sum(x => x.QuantityAllocatable * (Util.ProductDictionary[x.ItemNumber + 90000].ListPrice * 0.5));
                                double TotalBackOrderVal = I.Value.BackorderedItems.Sum(x => x.QuantityOnBackOrder * (Util.ProductDictionary[x.ItemNumber + 90000].ListPrice * 0.5));
                                sw.WriteLine("<tr><td>#{6}</td><td>{0}</td><td>{1} - {2}</td><td>{3}%</td><td>{4}</td><td colspan=\"" + (Cols - 7) + "\"><span class=\"f-right\">{7}</span></td><td colspan=\"" + (Cols - 6) + "\"><span class=\"f-right\">{8}<span></td></tr>",
                                    I.Key,
                                    I.Value.PurchaseOrderNumber,
                                    I.Value.OrderDate.HasValue ? I.Value.OrderDate.Value.ToString("MM/dd/yyyy") : I.Value.OrderDateRaw,
                                    I.Value.Discount,
                                    I.Value.BackorderedItems.Count.ToString(),
                                    (I.Value.BackorderedItems.Count > 1 ? "s" : " "),
                                    BackorderIndex.ToString(),
                                    CanShipVal.ToString("C"),
                                    TotalBackOrderVal.ToString("C"));
                                sw.WriteLine("</td></tr>");
                                CanShipSum += CanShipVal;
                                TotalSum += TotalBackOrderVal;
                                BackorderIndex++;
                            }
                            else if (ReportType == "5")
                            {
                                sw.WriteLine();
                                sw.WriteLine("#{6},Invoice #:{0},PO#:{1},{2},Discount {3}%,{4} Backordered Item{5},Expected Completion Date: {7}",
                                    I.Key,
                                    I.Value.PurchaseOrderNumber,
                                    I.Value.OrderDate.HasValue ? I.Value.OrderDate.Value.ToString("MM/dd/yyyy") : I.Value.OrderDateRaw,
                                    I.Value.Discount,
                                    I.Value.BackorderedItems.Count.ToString(),
                                    (I.Value.BackorderedItems.Count > 1 ? "s" : " "),
                                    BackorderIndex.ToString(),
                                    EstimatedShip.HasValue ? EstimatedShip.Value.ToString("MM/dd/yyyy") : "Unknown");
                                sw.WriteLine("Item,Number,Description,Ordered,Fill Qty");
                                BackorderIndex++;
                                foreach (BackorderedItem B in I.Value.BackorderedItems)
                                {
                                    //DateTime ShipDate = DateTime.Now;
                                    int Allocated = ProductAllocation.AllocatedItems[ProductAllocationModel.ProductNumber(B.ItemNumber)].AllocatedOnOrder(B.InvoiceNumber);
                                    #region Shipdate calculator, depracated
                                    //if (Allocated < B.QuantityOnBackOrder)
                                    //{
                                    //    var InvoicesBeforeCurrent = AllInvoices.Where(x => x.OrderDate <= (Util.InvoicePriorities.ContainsKey(I.Key) ? Util.InvoicePriorities[I.Key] : I.Value.OrderDate));
                                    //    int NeedToAssemble = InvoicesBeforeCurrent
                                    //        .Sum(x =>
                                    //            x.BackorderedItems
                                    //                .Where(x1 => x1.ItemNumber == B.ItemNumber)
                                    //                .Select(x2 => x2.QuantityOnBackOrder)
                                    //                .Sum() -
                                    //                x.BackorderedItems.Where(x1 => x1.ItemNumber == B.ItemNumber)
                                    //                .Select(x2 => x2.QuantityAllocatable)
                                    //                .Sum()
                                    //            );
                                    //    double Days = 0;
                                    //    if (NeedToAssemble > 0)
                                    //    {
                                    //        double seconds = (NeedToAssemble * Util.ProductDictionary[B.ItemNumber + 90000].AssemblyTime);
                                    //        //Util.Products.Where(x => x.RequiredProducts.ContainsKey(x.Number - 90000)).ToList().ForEach(item =>
                                    //        //{
                                    //        //    seconds += GetSeconds(ValidInvoices, item.Number, (DateTime)I.Value.OrderDate);
                                    //        //});
                                    //        //foreach (var AttachedProduct in Util.ProductDictionary[B.ItemNumber + 90000].RequiredProducts)
                                    //        //{
                                    //        //    seconds += (NeededToAssemble * Util.ProductDictionary[AttachedProduct.Key + 90000].AssemblyTime);
                                    //        //    seconds += ValidInvoices
                                    //        //            .Where(x => x.Value.OrderDate <= I.Value.OrderDate)
                                    //        //            .Sum(x => x.Value.BackorderedItems.Where(x1 => x1.ItemNumber == B.ItemNumber).Select(x2 => x2.QuantityOnBackOrder).Sum() - x.Value.BackorderedItems.Where(x1 => x1.ItemNumber == B.ItemNumber).Select(x2 => x2.QuantityAllocatable).Sum())
                                    //        //        * Util.ProductDictionary[AttachedProduct.Key + 90000].AssemblyTime;
                                    //        //}
                                    //        Days = Math.Round(((seconds / 3600) / 8) + 0.5, 0, MidpointRounding.AwayFromZero);
                                    //        while (Days > 0)
                                    //        {
                                    //            ShipDate = ShipDate.AddDays(1);
                                    //            while (ShipDate.DayOfWeek == DayOfWeek.Sunday || ShipDate.DayOfWeek == DayOfWeek.Saturday)
                                    //                ShipDate = ShipDate.AddDays(1);
                                    //            Days--;
                                    //        }
                                    //    }
                                    //} 
                                    #endregion
                                    sw.WriteLine("{0},{1},{2},{3},{4}",
                                        Count,
                                        ProductAllocationModel.ProductNumber(B.ItemNumber),
                                        B.ItemName,
                                        B.QuantityOnBackOrder.ToString(),
                                        Allocated.ToString()
                                        //,ShipDate == DateTime.Now ? "     Now" : ShipDate.ToString("MM/dd/yyyy")
                                        );
                                    Count++;
                                }
                            }
                            else
                            {
                                //<span class=\"xs\"></span><span class=\"xs\"></span>
                                sw.WriteLine("</tbody><tbody class=\"inv-" + I.Key + "\"><tr><td class=\"no-vis\" colspan=\"" + Cols + "\" class=\"non-vis\"><br></td></tr>");
                                sw.WriteLine("<tr><td class=\"toggler\" onmouseup=\"this.parentElement.parentElement.classList.contains('no-print') ? this.parentElement.parentElement.classList.remove('no-print') : this.parentElement.parentElement.classList.add('no-print')\"></td><td>#{6}</td><td colspan=\"" + (Cols - 3) + "\">Invoice #:{0} PO#:{1} - {2}</td><td colspan=\"" + (Cols - 5) + "\">Discount {3}%</td></tr><tr><td colspan=\"" + (Cols - 4) + "\">{4} Backordered Item{5}<td colspan=\"" + (Cols - 3) + "\">Expected Completion Date: {7}</td>",
                                    I.Key,
                                    Util.Pad(Side.Right, 12, I.Value.PurchaseOrderNumber),
                                    Util.Pad(Side.Right, 12, I.Value.OrderDate.HasValue ? I.Value.OrderDate.Value.ToString("MM/dd/yyyy") : I.Value.OrderDateRaw),
                                    I.Value.Discount,
                                    Util.Pad(Side.Left, 5, I.Value.BackorderedItems.Count.ToString()),
                                    (I.Value.BackorderedItems.Count > 1 ? 's' : ' '),
                                    Util.Pad(Side.Right, 4, BackorderIndex.ToString()),
                                    EstimatedShip.HasValue ? EstimatedShip.Value.ToString("MM/dd/yyyy") : "Unknown");
                                sw.WriteLine("</tr><tr>");
                                sw.WriteLine("<td></td><td>Item</td><td>Number</td><td>Desc</td>{0}", (PublicFields ? "<td>AOnHand</td><td>Ordered</td><td class=\"fill-qty\">Fill Qty</td><td class=\"boxes\">Boxes</td>" : "<td>Ordered</td><td colspan=\"" + (Cols - 4) + "\"></td>"));
                                sw.WriteLine("</tr>");
                                BackorderIndex++;
                                foreach (BackorderedItem B in I.Value.BackorderedItems.Where(xx => (ReportType == "1" ? ProductAllocation.AllocatedItems[ProductAllocationModel.ProductNumber(xx.ItemNumber)].AllocatedOnOrder(I.Value.InvoiceNumber) > 0 || ProductAllocation.AllocatedItems[ProductAllocationModel.ProductNumber(xx.ItemNumber)].ActualOnHand < 0 : ReportType == "2" ? xx.QuantityAllocatable < xx.QuantityOnBackOrder : ProductAllocation.AllocatedItems.Keys.Contains(ProductAllocationModel.ProductNumber(xx.ItemNumber)))))
                                {
                                    sw.WriteLine(
                                        "<tr><td class=\"toggler\" onmouseup=\"this.parentElement.classList.contains('no-print') ? this.parentElement.classList.remove('no-print') : this.parentElement.classList.add('no-print')\"></td><td>{0}</td><td>{1}</td><td>{2}</td><td class=\"right\">{3}</td>" +
                                        (PublicFields ? "<td class=\"right\">{4}</td><td class=\"right fill-qty\" data-oval=\"{5}\"><span class=\"editable\" ondblclick=\"input_insert(this)\">{5}</span></td><td></td></tr>" : "<td colspan=\"" + (Cols - 4) + "\"></td></tr>"),
                                        Count,
                                        (90000 + B.ItemNumber).ToString(),
                                        B.ItemName,
                                        (PublicFields ? ProductAllocation.AllocatedItems[ProductAllocationModel.ProductNumber(B.ItemNumber)].ActualOnHand.ToString() : B.QuantityOnBackOrder.ToString()),
                                        (PublicFields ? B.QuantityOnBackOrder.ToString() : string.Empty),
                                        (PublicFields ? ProductAllocation.AllocatedItems[ProductAllocationModel.ProductNumber(B.ItemNumber)].AllocatedOnOrder(B.InvoiceNumber).ToString() : string.Empty));
                                    Count++;
                                }
                                sw.WriteLine("</tbody>");
                            }
                            TotalItems += Count;
                        }
                        if (ReportType == "4")
                            sw.WriteLine($"<tr><td colspan=\"{Cols - 3}\"></td><td colspan=\"{Cols - 7}\">Sum:<span class=\"f-right\">{CanShipSum.ToString("C")}</span></td><td colspan=\"{Cols - 6}\"><span class=\"f-right\">{TotalSum.ToString("C")}</span></td></tr>");
                        ShownBackorders += BackorderIndex;
                        if (ReportType != "5")
                            sw.WriteLine("<tr><td class=\"no-vis\" colspan=\"" + Cols + "\"><br><br></td></tr>");
                    }
                }
                if (TotalItems == 0 && ShownBackorders == 0)
                {
                    sw.WriteLine("Sorry! There aren't currently any backorders that can be filled with the given criteria.");
                    sw.WriteLine("Submit a request with no customer specified to see a full list of backorders that can be filled,");
                    sw.WriteLine("or run backorder report [9] which will show all current and open backorders.");
                }
                if (ReportType != "5")
                    sw.WriteLine("</table></body></html>");
            }
            Process.Start($"{ExportFile}");
            DumpActual(ProductAllocation);
        }

        private static double GetSeconds(Dictionary<int, Invoice> Invoices, int ProductNumber, DateTime DateMax)
        {
            return Invoices
                .Where(x => x.Value.OrderDate <= DateMax)
                .Sum(x => x.Value.BackorderedItems.Where(x1 => x1.ItemNumber == (ProductNumber > 90000 ? ProductNumber - 90000 : ProductNumber)).Select(x2 => x2.QuantityOnBackOrder).Sum() - x.Value.BackorderedItems.Where(x1 => x1.ItemNumber == (ProductNumber > 90000 ? ProductNumber - 90000 : ProductNumber)).Select(x2 => x2.QuantityAllocatable).Sum())
                * Util.ProductDictionary[(ProductNumber > 90000 ? ProductNumber : ProductNumber + 90000)].AssemblyTime;
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

            List<Invoice> AllInvoices = Customer.ExtractInvoices(Util.CustomerDictionary);

            ProductAllocationModel ExportOrderAllocation =
                new ProductAllocationModel(
                    AllInvoices,
                    Customer.ExtractInvoices(Util.CustomerDictionary.Where(x => x.Key.StartsWith("E")).Select(x => x.Value)));

            ProductAllocationModel DomOrderAllocation =
                new ProductAllocationModel(
                    AllInvoices,
                    Customer.ExtractInvoices(Util.CustomerDictionary.Where(x => !x.Key.StartsWith("E")).Select(x => x.Value)));

            using (StreamWriter sw = new StreamWriter(Paths.ExportTotalBackOrdered))
            {
                sw.WriteLine(ProductAllocation.TotalBackorderedItems);
                sw.WriteLine(ProductAllocation.TotalValueOfBackOrders);
                double ExpBkoVal = 0;
                foreach (KeyValuePair<int, int> item in ExportOrderAllocation.CannotShip)
                {
                    ExpBkoVal += Util.ProductDictionary.ContainsKey(item.Key + 90000) ? ((Util.ProductDictionary[item.Key + 90000].ListPrice * .5) * item.Value) : 0;
                }
                sw.WriteLine(ExpBkoVal);
                sw.WriteLine(ExportOrderAllocation.TotalValueOfShippable);

                double DomBkoVal = 0;
                foreach (KeyValuePair<int, int> item in DomOrderAllocation.CannotShip)
                {
                    DomBkoVal += Util.ProductDictionary.ContainsKey(item.Key + 90000) ? (Util.ProductDictionary[item.Key + 90000].ListPrice * .5 * item.Value) : 0;
                }
                sw.WriteLine(DomBkoVal);
                sw.WriteLine(DomOrderAllocation.TotalValueOfShippable);
            }
            using (StreamWriter sw = new StreamWriter(Paths.BackOrderedShipdate))
            {
                for (int i = 1; i <= Util.Products.Max(x => x.Id); i++)
                {
                    DateTime ShipDate = DateTime.Now;
                    double Days = 0;
                    if (ProductAllocation.CannotShip.ContainsKey(i))
                        Days = Math.Round((((ProductAllocation.CannotShip[i] * Util.ProductDictionary[i + 90000].AssemblyTime) / 3600) / 8) + 0.5, 0, MidpointRounding.AwayFromZero);

                    while (Days > 0)
                    {
                        ShipDate = ShipDate.AddDays(1);
                        while (ShipDate.DayOfWeek == DayOfWeek.Sunday || ShipDate.DayOfWeek == DayOfWeek.Saturday)
                            ShipDate = ShipDate.AddDays(1);
                        Days--;
                    }

                    sw.WriteLine($"{i + 90000}{(ShipDate != DateTime.Now ? ShipDate.ToString("MM/dd/yyyy") : "now")}");

                }
            }
        }
    }
}
