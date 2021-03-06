﻿using System;
using System.IO;
using System.Linq;
using System.Resources;
using System.Collections;
using System.Globalization;
using System.Collections.Generic;
using static AssemblyPrintout.Datatypes;

namespace AssemblyPrintout
{
    public enum Side
    {
        Left,
        Right
    }
    static class Util
    {
        private static bool? JobberOnline = null;
        public static bool SourceSalesIsOnline => (bool)(JobberOnline = JobberOnline ?? Directory.Exists(Paths.SourceSales));

        private static bool? SourceOnline = null;
        public static bool SourceInvenIsOnline => (bool)(SourceOnline = SourceOnline ?? Directory.Exists(Paths.SourceInven));

        private static bool? DevMode = null;
        public static bool InDevMode(bool EnableDev = false) => (bool)(DevMode = DevMode ?? EnableDev);

        #region Data containers and Suppliers

        private static Dictionary<DateTime, double> EDSData = null;
        public static Dictionary<DateTime, double> EDS => EDSData = EDSData ?? GetEdsData();

        private static List<ProductModel> ProductData = null;
        public static List<ProductModel> Products => ProductData = ProductData ?? GetProductData();
        private static Dictionary<int, ProductModel> ProductDictData = null;
        public static Dictionary<int, ProductModel> ProductDictionary => ProductDictData = ProductDictData ?? GetProductDictionary();

        private static List<PartModel> PartData = null;
        public static List<PartModel> Parts => PartData = PartData ?? GetPartData();
        public static Dictionary<int, PartModel> PartDictData = null;
        public static Dictionary<int, PartModel> PartDictionary => PartDictData = PartDictData ?? GetPartsDictionary();

        private static List<AssemblyKitModel> KitData = null;
        public static List<AssemblyKitModel> Kits => KitData = KitData ?? GetKitData();
        public static Dictionary<int, AssemblyKitModel> KitDictData = null;
        public static Dictionary<int, AssemblyKitModel> KitDictionary => KitDictData = KitDictData ?? GetKitDictionary();

        private static Dictionary<string, Customer> CustomerDictData = null;
        public static Dictionary<string, Customer> CustomerDictionary => CustomerDictData = CustomerDictData ?? GetInvoiceData();

        private static Dictionary<int, DateTime> Priorities = null;
        public static Dictionary<int, DateTime> InvoicePriorities => Priorities = Priorities ?? GetBkoPriority();

        public static List<string> PartPrefixFilter = new List<string> { "LB", "MR" };
        public static List<int> PartNumberFilter = new List<int> { 1698 };

        public static double Inv3600 = .00027777777777777778;
        public static double Inv365 = 0.00273972602739726027397260273973;
        static readonly char[] Numbers = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

        static string Space => "                                                   ";
        static string DashS => "───────────────────────────────────────────────────";
        private static string PadL(int length = -1, string S = "", bool Dash = false) => length - S.Length >= 0 ? (!Dash ? Space : DashS).Substring(0, length - S.Length) + S : (!Dash ? Space : DashS) + S;
        private static string PadR(int length = -1, string S = "", bool Dash = false) => length - S.Length >= 0 ? S + (!Dash ? Space : DashS).Substring(0, length - S.Length) : S + (!Dash ? Space : DashS);

        public static string Pad(Side? side = null, int length = -1, string S = "", bool Dash = false) => side == null ? Space : side == Side.Left ? PadL(length, S, Dash) : PadR(length, S, Dash);


        private static readonly CultureInfo CultureHolder = new CultureInfo("en-US");
        public static CultureInfo Culture => CultureHolder;
        #endregion
        #region Initialize and populate Data containers for parts, products and kits

        private static Dictionary<DateTime, double> GetEdsData()
        {
            var ReturnData = new Dictionary<DateTime, double>();
            Read.GenericRead(Paths.EdsHistoryData).ToList().ForEach(x =>
            {
                var s = x.Split(',');
                if (s.Length == 2 && DateTime.TryParse(s[0], out DateTime d) && double.TryParse(s[1], out double dbl))
                    ReturnData.Add(d, dbl);
            });
            return ReturnData;
        }

        /// <summary>
        /// Initializes and populates ProductData and ProductDictData with {SourceDir}\TEMPDATA\PRODDUMP.TXT.
        /// </summary>
        /// <returns>List of ProductModel items.</returns>
        private static List<ProductModel> GetProductData()
        {
            var Data = new List<ProductModel>();
            int index = 1;
            foreach (string Line in Read.GenericRead($@"{Paths.SourceDir}\TEMPDATA\PRODDUMP.TXT"))
            {
                string[] items = Line.Split('ð');
                if (items.Length == 55 && int.TryParse(items[0], out int i1))
                {
                    var newProd = new ProductModel(i1, items[1], double.TryParse(items[2], out double i2) ? i2 : 0, double.TryParse(items[3], out double i3) ? i3 : 0, double.TryParse(items[4], out double i4) ? i4 : 0, double.TryParse(items[5], out double i5) ? i5 : 0, int.TryParse(items[6], out int i6) ? i6 : 0, int.TryParse(items[7], out int i7) ? i7 : 0, int.TryParse(items[8], out int i8) ? i8 : 0, int.TryParse(items[9], out int i9) ? i9 : 0, int.TryParse(items[10], out int i10) ? i10 : 0, int.TryParse(items[11], out int i11) ? i11 : 0, int.TryParse(items[12], out int i12) ? i12 : 0);
                    for (int i = 13; i < 53; i += 2) newProd.AddRequiredItem(items[i], items[i + 1]);
                    if (int.TryParse(items[54], out int SA)) newProd.AssemblyKits.Add(SA, 1);
                    Data.Add(newProd);
                }
                else throw new Exception($"Empty Product Record: Check Product #{(index + 90000)}");
                index++;
            }
            ProductDictData = ProductDictData ?? new Dictionary<int, ProductModel>();
            Data.ForEach(x => { if (!ProductDictData.ContainsKey(x.Number)) ProductDictData.Add(x.Number, x); });
            return Data;
        }

        /// <summary>
        /// If ProductDictionary is called prior to being initialized, ProductData and ProductDictData are populated.
        /// </summary>
        /// <returns>Initalized and populated ProductDictData Dictionary</returns>
        private static Dictionary<int, ProductModel> GetProductDictionary()
        {
            ProductData = GetProductData();
            return ProductDictData;
        }

        /// <summary>
        /// Initializes and populates PartData and PartDictData with {SourceDir}\TEMPDATA\PARTDUMP{i}.TXT.
        /// </summary>
        /// <returns>List of PartModel items.</returns>
        private static List<PartModel> GetPartData()
        {
            List<PartModel> Data = new List<PartModel>();
            PartModel Part;
            for (int i = 0; i < 4; i++)
                foreach (var LineItem in Read.GenericRead(Paths.ImportPartDump(i)))
                {
                    Part = null;
                    if ((Part = new PartModel(LineItem.Split('ð'))) != null && Part != new PartModel() && Part.PartNumber > 0)
                        Data.Add(Part);
                }
            PartDictData = PartDictData ?? new Dictionary<int, PartModel>();
            Data.ForEach(x => { if (!PartDictData.ContainsKey(x.PartNumber)) PartDictData.Add(x.PartNumber, x); });
            return Data;
        }

        /// <summary>
        /// If PartDictionary is called prior to being initialized, PartData and PartDictData are populated.
        /// </summary>
        /// <returns>Initalized and populated PartDictData Dictionary</returns>
        private static Dictionary<int, PartModel> GetPartsDictionary()
        {
            PartData = GetPartData();
            return PartDictData;
        }

        /// <summary>
        /// Initializes and populates KitData and KitDictData with {SourceDir}\TEMPDATA\STRNLNG.TXT.
        /// </summary>
        /// <returns>List of AssemblyKitModel items.</returns>
        private static List<AssemblyKitModel> GetKitData()
        {
            int index = 0;
            List<AssemblyKitModel> Data = new List<AssemblyKitModel>();
            foreach (string D in Read.GenericRead($@"{Paths.SourceDir}\TEMPDATA\STRNLNG.TXT"))
            {
                string[] items = D.Split('ð');
                if (items.Length == 42)
                {
                    int? SecondaryId = null;
                    Dictionary<int, int> TempParts = new Dictionary<int, int>();
                    Dictionary<int, int> TempProducts = new Dictionary<int, int>();
                    for (int i = 1; i < 41; i += 2)
                    {
                        PartModel Part;
                        ProductModel Product;
                        if (items[i + 1] == "P" && int.TryParse(items[i], out int ProductNumber) && ProductDictionary.ContainsKey(ProductNumber) && (Product = ProductDictionary[ProductNumber]) != null)
                        {
                            if (!TempProducts.ContainsKey(Product.Id)) TempProducts.Add(Product.Id, 0);
                            TempProducts[Product.Id] += 1;
                        }
                        else if (int.TryParse(items[i], out int PartNumber) && PartNumber > 0 && int.TryParse(items[i + 1], out int PartQuantity) && PartDictionary.ContainsKey(PartNumber) && (Part = PartDictionary[PartNumber]) != null)
                        {
                            if (!TempParts.ContainsKey(Part.Id)) TempParts.Add(Part.Id, 0);
                            TempParts[Part.Id] += PartQuantity;
                        }
                    }
                    SecondaryId = int.TryParse(items[41], out int i4) ? i4 : 0;
                    Data.Add(new AssemblyKitModel(index++, items[0], string.Empty, SecondaryId, TempParts, TempProducts));
                }
            }
            KitDictData = KitDictData ?? new Dictionary<int, AssemblyKitModel>();
            Data.ForEach(x => { if (!KitDictData.ContainsKey(x.AssemblyKitNumber)) KitDictData.Add(x.AssemblyKitNumber, x); });
            return Data;
        }

        /// <summary>
        /// If KitDictionary is called prior to being initialized, KitData and KitDictData are populated.
        /// </summary>
        /// <returns>Initalized and populated KitDictData Dictionary</returns>
        private static Dictionary<int, AssemblyKitModel> GetKitDictionary()
        {
            KitData = GetKitData();
            return KitDictData;
        }

        private static Dictionary<int, DateTime> GetBkoPriority()
        {
            Dictionary<int, DateTime> Priority = new Dictionary<int, DateTime>();
            Read.GenericRead(@"\\source\inven\TEMPDATA\BkoPriority.txt").ToList().ForEach(item =>
                    {
                        string[] temp = item.Split(',');
                        if (temp.Length == 2 && int.TryParse(temp[0], out int BkoNum) && DateTime.TryParse(temp[1], out DateTime InjectDate))
                            Priority.Add(BkoNum, InjectDate);
                    });
            return Priority;
        }

        /// <summary>
        /// An ugly turd of a function that parses customers and invoices and organizes them into models.
        /// </summary>
        /// <param name="Customers">List of customerss obtained by previous function.</param>
        /// <returns>Dicionary<string, Customer>(CustomerCode, CustomerModel)</returns>
        public static Dictionary<string, Customer> GetInvoiceData()
        {
            Dictionary<int, string> FileData = new Dictionary<int, string>();
            // Get all invoice files from jobber pc and put into Dict<Invoice#,Path> to be used later
            var InvoiceFiles = FileManager.GetDirFiles($@"{Paths.SalesDir}\{DateTime.Now.Year}").ToList();
            foreach (string f in InvoiceFiles)
                if (f.Length > 7 && int.TryParse(f.Split('\\').Last().Substring(0, f.Split('\\').Last().Length - 7), out int InvoiceNum) && !FileData.ContainsKey(InvoiceNum))
                    FileData.Add(InvoiceNum, f);

            string BKOIntegrity = string.Empty;
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
                            BKOIntegrity += BKOIntegrity == string.Empty ? $"{InvoiceNumber}" : $", {InvoiceNumber}";
                            //Environment.Exit(-1);
                            break;
                        }
                    }
                }
            };
            using (StreamWriter sw = new StreamWriter(Paths.BKOIntegrity))
                if (BKOIntegrity == string.Empty) sw.WriteLine("0"); else sw.WriteLine(BKOIntegrity);

            //return Customers;

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

                foreach (var x in C.Value.Invoices.OrderBy(x => x.Value.InvoiceNumber))
                    if (!C.Value.Invoices.ContainsKey(x.Key))
                        C.Value.Invoices.Add(x.Key, x.Value);
            }
            return Customers;
        }
        #endregion
        #region Logging

        /// <summary>
        /// Posts "Message" to log file on all error log servers.
        /// </summary>
        /// <param name="Message">A string containing message to be logged.</param>
        /// <param name="errorType">Selected enum of where to log the error.</param>
        public static void Log(string Message)
        {
            GetValues<ErrorType>().ForEach(e => { using (StreamWriter sw = GetErrorWriter(e)) sw.WriteLine($"{DateTime.Now} @ {Environment.MachineName}\\{Environment.UserName} - {Message}"); });
        }

        /// <summary>
        /// Posts "Message" to log file on server of "errortype".
        /// </summary>
        /// <param name="Message">A string containing message to be logged.</param>
        /// <param name="errorType">Selected enum of where to log the error.</param>
        public static void Log(string Message, ErrorType errorType)
        {
            using (StreamWriter sw = GetErrorWriter(errorType)) sw.WriteLine($"{DateTime.Now} @ {Environment.MachineName}\\{Environment.UserName} - {Message}");
        }

        /// <summary>
        /// Posts "Message" to log file on server of "errortype".
        /// </summary>
        /// <param name="Message">A string containing message to be logged.</param>
        /// <param name="errorType">Selected enums of where to log the error.</param>
        public static void Log(string Message, IEnumerable<ErrorType> errorType)
        {
            foreach (var e in errorType) using (StreamWriter sw = GetErrorWriter(e)) sw.WriteLine($"{DateTime.Now} @ {Environment.MachineName}\\{Environment.UserName} - {Message}");
        }

        /// <summary>
        /// Retrieves StreamWriter for error logging locations.
        /// </summary>
        /// <param name="e">Logging destination to be opened.</param>
        /// <returns>new StreamWriter</returns>
        public static StreamWriter GetErrorWriter(ErrorType e) => new StreamWriter((e == ErrorType.CSharpError ? Paths.CSError : e == ErrorType.JobberError ? Paths.JobberError : e == ErrorType.QBError ? Paths.QBError : Paths.CSError), true);

        /// <summary>
        /// Creates log(s), Exits Program with -1 (error) and Throws an Exception.
        /// </summary>
        /// <param name="ErrorMessage">Message to write to log files.</param>
        /// <param name="CSharpAndQBLogOnly">Should message be written to all logs or only CSharp and QB logs. (Default: true)</param>
        public static void ExceptionExit(string ErrorMessage, Exception Ex = null, bool CSharpAndQBLogOnly = true)
        {
            if (!string.IsNullOrEmpty(ErrorMessage.Trim()))
            {
                if (CSharpAndQBLogOnly) Log($"{ErrorMessage}\n\r{Ex.Message}", new ErrorType[] { ErrorType.CSharpError, ErrorType.QBError });
                else Log($"{ErrorMessage}\n\r{Ex.Message}");
            }
            else if (Ex != null)
            {
                if (CSharpAndQBLogOnly) Log($"Exception:{Ex.Message}", new ErrorType[] { ErrorType.CSharpError, ErrorType.QBError });
                else Log($"Exception:{Ex.Message}");
            }
            Environment.Exit(-1);
            throw Ex;
        }
        #endregion
        #region Misc

        public static string CSS =>
            "* { font-family:\"Courier New\"; font-weight:600; font-size:10pt; letter-spacing: -0.4px; transition: 0.25s background; } " +
            ".editable { width: 100%; display: inline-block; cursor: pointer; } " +
            ".fill-qty, .fill-qty input { width: 75px; } " +
            "table {border-collapse: collapse; width:680px; } thead { display: table-row-group; } " +
            "td, th { border: 1px solid #000;padding: 2px;} td { white-space: nowrap; overflow: hidden; } " +
            ".right { text-align:right; } " +
            ".f-right {float:right;} " +
            ".no-vis, .no-vis * { border: none; } " +
            ".boxes { min-width:75px; } " +
            ".none { display:none; } " +
            ".no-print, .no-print * { background:#CCC; } " +
            ".toggler { width: 15px; } " +
            ".toggler:hover, .no-print .toggler { background: #868686; } " +
            ".no-print .no-vis, .no-print .no-vis * { background: #fff; }" +
            "@media print { .no-print, .no-print * { display: none !important; } }";

        public static string Script => "function span_insert(obj, forceOldVal) {\n" +
            "\tobj.outerHTML = '<span class=\"editable\" ondblclick=\"input_insert(this)\">' + (isNaN(obj.value) || obj.value == undefined || forceOldVal ? parseInt(obj.parentElement.dataset.oval) : obj.value)  + '</span>';\n" +
            "}\n" +
            "function input_insert(obj) {\n" +
            "\tlet tempid = Date.now();\n" +
            "\tobj.outerHTML = '<input id=\"' + tempid + '\" type=\"number\" value=\"' + (isNaN(obj.innerHTML) || obj.innerHTML == undefined ? parseInt(obj.parentElement.dataset.oval) : parseInt(obj.innerHTML)) + '\" onkeyup=\"(event.which || event.keyCode) == 13 ? span_insert(this, false) : (event.which || event.keyCode) == 27 ? span_insert(this, true) : null;\" />';\n" +
            "\tdocument.getElementById(tempid).focus();\n" +
            "}\n" +
            "function span_text_insert(obj, forceOldVal) {\n" +
            "\tobj.outerHTML = '<span class=\"editable\" ondblclick=\"input_text_insert(this)\">' + (obj.value == undefined || forceOldVal ? obj.parentElement.dataset.oval : obj.value)  + '</span>';\n" +
            "}\n" +
            "function input_text_insert(obj) {\n" +
            "\tlet tempid = Date.now();\n" +
            "\tobj.outerHTML = '<input id=\"' + tempid + '\" type=\"text\" value=\"' + (obj.innerHTML == undefined ? obj.parentElement.dataset.oval : obj.innerHTML == 'Employee' ? '' : obj.innerHTML) + '\" onkeyup=\"(event.which || event.keyCode) == 13 ? span_text_insert(this, false) : (event.which || event.keyCode) == 27 ? span_text_insert(this, true) : null;\" />';\n" +
            "\tdocument.getElementById(tempid).focus();\n" +
            "}\n";
        /// <summary>
        /// Posts to active write file a warning stating that development data is being used, not live data.
        /// </summary>
        /// <param name="sw">An active StreamWriter</param>
        public static void JobberOfflineWarning(StreamWriter sw)
        {
            if (!SourceSalesIsOnline)
            {
                sw.WriteLine();
                sw.WriteLine(@"***************************** WARNING! YOUR PC IS UNABLE TO CONNECT TO \\SOURCE\SALES      *****************************");
                sw.WriteLine(@"***************************** WARNING! THE DATA IN THE FOLLOWING REPORT IS NOT VALID       *****************************");
                sw.WriteLine(@"***************************** WARNING! IF THIS ERROR PERSISTS, CONTACT AN ADMINISTRATOR    *****************************");
                sw.WriteLine();
            }
        }
        public static void SourceOfflineWarning(StreamWriter sw)
        {
            if (InDevMode() || !SourceInvenIsOnline)
            {
                sw.WriteLine();
                sw.WriteLine(@"***************************** WARNING! YOUR PC IS UNABLE TO CONNECT TO \\SOURCE\INVEN      *****************************");
                sw.WriteLine(@"***************************** WARNING! THE DATA IN THE FOLLOWING REPORT IS NOT VALID       *****************************");
                sw.WriteLine(@"***************************** WARNING! IF THIS ERROR PERSISTS, CONTACT AN ADMINISTRATOR    *****************************");
                sw.WriteLine();
            }
        }

        /// <summary>
        /// Returns (int) how many days have occurred since June 30th, this year.
        /// </summary>
        public static int Getj30 => DateTime.Now.Subtract(DateTime.Parse($"06/30/{(DateTime.Now.Month <= 6 ? DateTime.Now.Year - 1 : DateTime.Now.Year)}")).Days;

        /// <summary>
        /// Returns fiscal year - June is switch date.
        /// </summary>
        public static int GetFiscalYear => (DateTime.Now.Month <= 6) ? DateTime.Now.Year - 1 : DateTime.Now.Year;

        /// <summary>
        /// Updates resource file to contain "This month last year's" average daily hours of production. Only updates resource if new month.
        /// </summary>
        /// <param name="force">If forced, the resource file will be updated regardless of the last time it was updated.</param>
        public static void UpdateAvgs(bool force = false)
        {
            DateTime? LastUpdate = null;
            IDictionaryEnumerator dict = null;
            ResXResourceReader resx = new ResXResourceReader("Resource1.resx");

        TryGetResx:
            try
            {
                dict = resx.GetEnumerator();
            }
            catch
            {
                UpdateResourceFile();
            }
            if (dict == null) goto TryGetResx;
            while (dict.MoveNext()) if ((string)dict.Key == "ProdAvgLastUpdate") LastUpdate = (DateTime?)dict.Value;

            if ((LastUpdate != null && LastUpdate.HasValue && (LastUpdate.Value.Month < DateTime.Now.Month || (LastUpdate.Value.Month == 1 && DateTime.Now.Month == 12))) || force) UpdateResourceFile();

        }

        public static bool GetDateTimeFromArg(string ArgVal, out DateTime Date)
        {
            string[] DateComponents = ArgVal.Split('-');
            return DateTime.TryParse($"{DateComponents[0]}/{DateComponents[1]}/{(20 + DateComponents[2])}", out Date);
        }

        /// <summary>
        /// Actually updates the resource file with new production averages.
        /// </summary>
        static void UpdateResourceFile()
        {
            using (ResXResourceWriter resxWriter = new ResXResourceWriter("Resource1.resx"))
            {
                for (int Month = 1; Month < 13; Month++) resxWriter.AddResource($"ProductionAvg{Month}", (decimal)GetDailyAvgForMonth(Month));
                resxWriter.AddResource("ProdAvgLastUpdate", DateTime.Now.Date);
            };
        }

        /// <summary>
        /// Gets Daily production Average for month of (Month,) last year.
        /// </summary>
        /// <param name="Month">Month Number, 1-12</param>
        /// <returns>(double) Daily production Average for (Month)</returns>
        public static double GetDailyAvgForMonth(int Month)
        {
            var AssemblyData = Read.GenericRead($@"{Paths.SourceDir}\PRODUCTS{Month}.BAK");
            /// dict<productnumber, number assembled></productnumber>
            Dictionary<int, int> Assembled = new Dictionary<int, int>();
            double SecondsOfProduction = 0;
            AssemblyData.ToList().ForEach(item =>
            {
                var Item = new ProductionItem(item);
                if (!Assembled.ContainsKey(Item.ProductNumber)) Assembled.Add(Item.ProductNumber, 0);
                Assembled[Item.ProductNumber] += Item.NumberAssembled;
            });
            Products.ForEach(item =>
            {
                if (Assembled.ContainsKey(item.Number)) SecondsOfProduction += (Assembled[item.Number] * item.AssemblyTime);
            });
            return (SecondsOfProduction * .00027777777778) * (1 / WeekdaysInMonth(Month));
        }

        public static NeededAndAnnual GetAnnualUseHours()
        {
            NeededAndAnnual naa = new NeededAndAnnual
            {
                AnnualHours = 0,
                Needed30 = 0,
                Needed60 = 0
            };
            foreach (ProductModel Product in Util.Products)
            {
                naa.AnnualHours += ((Product.AnnualUse * Product.AssemblyTime) * Util.Inv3600);
                double daily = Product.AnnualUse * 0.0027397260273973;
                naa.Needed30 += (((daily * 30) - Product.QuantityOnHand) * Product.AssemblyTime) * Util.Inv3600;
                naa.Needed60 += (((daily * 60) - Product.QuantityOnHand) * Product.AssemblyTime) * Util.Inv3600;
            }
            naa.AnnualHours = Math.Round(naa.AnnualHours, 2, MidpointRounding.AwayFromZero);
            naa.Needed30 = Math.Round(naa.Needed30, 2, MidpointRounding.AwayFromZero);
            naa.Needed60 = Math.Round(naa.Needed60, 2, MidpointRounding.AwayFromZero);
            return naa;
        }

        /// <summary>
        /// Takes sum of hours needed to produce a years supply of products and divides it by available work days. (Hours needed per day)
        /// </summary>
        public static double HoursNeededPerDayForYearsSupply => GetAnnualUseHoursSum * 0.0037735849056604;

        /// <summary>
        /// Sums assembly hours needed to produce a years supply of products.
        /// </summary>
        public static double GetAnnualUseHoursSum => Products.Sum(Product => (((Product.AnnualUse - Product.QuantityOnHand) * Product.AssemblyTime) * Util.Inv3600));

        /// <summary>
        /// Counts number of weekdays (Mon-Fri) in month (Month)
        /// </summary>
        /// <param name="Month">Month Number, 1-12</param>
        /// <returns>(int) Weekdays</returns>
        public static int WeekdaysInMonth(int Month)
        {
            int WeekDays = 0;
            var FirstOfMonth = DateTime.Parse($@"{(Month < 10 ? $"0{Month}" : $"{Month}")}/01/{(DateTime.Now.Year - 1)}");
            for (int Day = 0; Day <= DateTime.DaysInMonth(DateTime.Now.Year - 1, Month); Day++)
            {
                switch (FirstOfMonth.AddDays(Day).DayOfWeek)
                {
                    case DayOfWeek.Saturday:
                    case DayOfWeek.Sunday: break;
                    default:
                        WeekDays++;
                        break;
                }
            }
            return WeekDays;
        }

        /// <summary>
        /// Calculates hours of production yesterday.
        /// </summary>
        /// <param name="ProductionData">Production data from production file.</param>
        /// <param name="Products">Product data [ProductNumber, AssemblyTime in seconds]</param>
        /// <returns>Value of yesterday's production hours, rounded to nearest hundreth.</returns>
        private static double GetYesterdayOnly(IEnumerable<string> ProductionData)
        {
            double yesterdayHours = 0;
            bool HasRunAgain = false;
        RunAgain:
            DateTime? yest = null;
            int YestCount = 0;
            var data = ProductionData.ToList();
            for (int i = data.Count - 1; i >= 0; i--)
            {
                if (data[i].Length >= 55 && DateTime.TryParse(data[i].Substring(data[i].Length - 19, 10), out DateTime ProductionDate) && ProductionDate.Date < DateTime.Today.Date)
                {
                    if (ProductionDate.Date == (yest = yest ?? ProductionDate).Value.Date && int.TryParse(data[i].Substring(49, 6), out int produced) && int.TryParse(data[i].Substring(0, 5), out int ProductNumber) && Util.ProductDictionary.ContainsKey(ProductNumber))
                    {
                        yesterdayHours += (double)(produced * (ProductDictionary[ProductNumber].AssemblyTime * .00027777777778M));
                        YestCount++;
                    }
                    else if (yest != null && ProductionDate.Date < yest)
                        break;
                }
            }
            if (YestCount == 0 && !HasRunAgain)
            {
                ProductionData = Read.GenericRead(Paths.ImportProductionX(DateTime.Now.Date.Month == 1 ? 12 : DateTime.Now.Date.Month - 1));
                HasRunAgain = true;
                goto RunAgain;
            }
            return Math.Round(yesterdayHours, 2, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Calculates yesterdays's production hours and updates file containing values, if it has not been updated yet, resets to Zero if nothing has been produced yet today.
        /// </summary>
        /// <param name="CurrentData">Current production values.</param>
        /// <param name="ProductData">Product data [ProductNumber, AssemblyTime in seconds]</param>
        /// <returns>Updated value for Hours of production</returns>
        public static double GetHoursAlt(double[] CurrentData, bool Force) => (File.GetLastWriteTime(Paths.ExportInitPath).Date < DateTime.Now.Date || CurrentData[1] == 0 || Force) ? GetYesterdayOnly(Read.GenericRead(Paths.ImportProduction)) : 0;

        /// <summary>
        /// Cleaner Implementaions of Enum.GetValues() function.
        /// </summary>
        /// <typeparam name="T">enum Type</typeparam>
        /// <returns>List of enum values.</returns>
        public static List<T> GetValues<T>() => Enum.GetValues(typeof(T)).Cast<T>().ToList();
        #endregion
    }

}
