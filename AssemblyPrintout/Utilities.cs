using System;
using System.IO;
using Microsoft.Win32;
using System.Resources;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;
using static AssemblyPrintout.Datatypes;
using System.Linq;
using System.Collections;

namespace AssemblyPrintout
{
    static class Utilities
    {
        private static bool? JobberOnline = null;
        public static bool SourceSalesIsOnline => (bool)(JobberOnline = JobberOnline ?? Directory.Exists(Paths.SourceSales));

        private static bool? SourceOnline = null;
        public static bool SourceInvenIsOnline => (bool)(SourceOnline = SourceOnline ?? Directory.Exists(Paths.SourceInven));

        private static bool? DevMode = null;
        public static bool InDevMode(bool EnableDev = false) => (bool)(DevMode = DevMode ?? EnableDev);
        #region Data containers and Suppliers

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

        public static List<string> PartPrefixFilter = new List<string> { "LB", "MR" };
        public static List<int> PartNumberFilter = new List<int> { 1698 };

        public static double Inv3600 = .00027777777777777778;
        public static double Inv365 = 0.00273972602739726027397260273973;

        private static CultureInfo CultureHolder = new CultureInfo("en-US");
        public static CultureInfo Culture => CultureHolder;
        #endregion
        #region Initialize and populate Data containers for parts, products and kits

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
                foreach (var LineItem in Read.GenericRead($@"{Paths.SourceDir}\TEMPDATA\PARTDUMP{i}.TXT"))
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

        /// <summary>
        /// Posts to active write file a warning stating that development data is being used, not live data.
        /// </summary>
        /// <param name="sw">An active StreamWriter</param>
        public static void JobberOfflineWarning(StreamWriter sw)
        {
            if (!Utilities.SourceSalesIsOnline)
            {
                sw.WriteLine();
                sw.WriteLine("***************************** WARNING! YOUR PC IS UNABLE TO CONNECT TO THE JOBBER COMPUTER *****************************");
                sw.WriteLine("***************************** WARNING! THE DATA IN THE FOLLOWING REPORT IS NOT VALID       *****************************");
                sw.WriteLine("***************************** WARNING! IF THIS ERROR PERSISTS, CONTACT AN ADMINISTRATOR    *****************************");
                sw.WriteLine();
            }
        }
        public static void SourceOfflineWarning(StreamWriter sw)
        {
            if (Utilities.InDevMode() || !Utilities.SourceInvenIsOnline)
            {
                sw.WriteLine();
                sw.WriteLine("***************************** WARNING! YOUR PC IS UNABLE TO CONNECT TO THE SOURCE COMPUTER *****************************");
                sw.WriteLine("***************************** WARNING! THE DATA IN THE FOLLOWING REPORT IS NOT VALID       *****************************");
                sw.WriteLine("***************************** WARNING! IF THIS ERROR PERSISTS, CONTACT AN ADMINISTRATOR    *****************************");
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
            return (SecondsOfProduction / 3600) / WeekdaysInMonth(Month);
        }

        public static neededAndAnnual GetAnnualUseHours()
        {
            neededAndAnnual naa = new neededAndAnnual();
            naa.AnnualHours = 0;
            naa.Needed30 = 0;
            naa.Needed60 = 0;
            foreach (ProductModel Product in Utilities.Products)
            {
                naa.AnnualHours += ((Product.AnnualUse * Product.AssemblyTime) * Utilities.Inv3600);
                double daily = Product.AnnualUse / 365;
                naa.Needed30 += (((daily * 30) - Product.QuantityOnHand) * Product.AssemblyTime) * Utilities.Inv3600;
                naa.Needed60 += (((daily * 60) - Product.QuantityOnHand) * Product.AssemblyTime) * Utilities.Inv3600;
            }
            naa.AnnualHours = Math.Round(naa.AnnualHours, 2, MidpointRounding.AwayFromZero);
            naa.Needed30 = Math.Round(naa.Needed30, 2, MidpointRounding.AwayFromZero);
            naa.Needed60 = Math.Round(naa.Needed60, 2, MidpointRounding.AwayFromZero);
            return naa;
        }

        /// <summary>
        /// Takes sum of hours needed to produce a years supply of products and divides it by available work days. (Hours needed per day)
        /// </summary>
        public static double HoursNeededPerDayForYearsSupply => GetAnnualUseHoursSum / 265;

        /// <summary>
        /// Sums assembly hours needed to produce a years supply of products.
        /// </summary>
        public static double GetAnnualUseHoursSum => Utilities.Products.Sum(Product => (((Product.AnnualUse - Product.QuantityOnHand) * Product.AssemblyTime) * Utilities.Inv3600));

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
            DateTime? yest = null;
            var data = ProductionData.ToList();
            for (int i = data.Count - 1; i >= 0; i--)
            {
                if (data[i].Length >= 55 && DateTime.TryParse(data[i].Substring(data[i].Length - 19, 10), out DateTime ProductionDate) && ProductionDate.Date < DateTime.Today.Date)
                {
                    if (ProductionDate.Date == (yest = yest?.Date ?? ProductionDate.Date) && int.TryParse(data[i].Substring(49, 6), out int produced) && int.TryParse(data[i].Substring(0, 5), out int ProductNumber) && Utilities.ProductDictionary.ContainsKey(ProductNumber))
                        yesterdayHours += (double)(produced * (Utilities.ProductDictionary[ProductNumber].AssemblyTime / 3600M));
                    else if (yest != null && ProductionDate.Date < yest)
                        break;
                }
            }
            return Math.Round(yesterdayHours, 2, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Calculates yesterdays's production hours and updates file containing values, if it has not been updated yet, resets to Zero if nothing has been produced yet today.
        /// </summary>
        /// <param name="CurrentData">Current production values.</param>
        /// <param name="ProductData">Product data [ProductNumber, AssemblyTime in seconds]</param>
        /// <returns>Updated value for Hours of production</returns>
        public static double GetHoursAlt(double[] CurrentData) => (File.GetLastWriteTime(Paths.InitPath).Date < DateTime.Now.Date || CurrentData[1] == 0) ? GetYesterdayOnly(Read.GenericRead(Paths.Production)) : 0;

        /// <summary>
        /// Cleaner Implementaions of Enum.GetValues() function.
        /// </summary>
        /// <typeparam name="T">enum Type</typeparam>
        /// <returns>List of enum values.</returns>
        public static List<T> GetValues<T>() => Enum.GetValues(typeof(T)).Cast<T>().ToList();
        #endregion
    }
}
