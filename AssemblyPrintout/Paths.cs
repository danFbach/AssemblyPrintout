using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AssemblyPrintout
{
    static class Paths
    {
        public static string SourceDir => !Utilities.InDevMode() && Utilities.SourceSalesIsOnline && Utilities.SourceInvenIsOnline ? SourceInven : DevInven;
        public static string SalesDir => !Utilities.InDevMode() && Utilities.SourceSalesIsOnline ? SourceSales : DevSales;
        public static string SourceSales = @"\\192.168.0.194\sales";
        public static string DevSales = @"\\192.168.0.94\sales";
        public static string SourceInven = @"\\192.168.0.194\Inven";
        public static string DevInven = @"\\192.168.0.94\inven";
        public static string LocalPath = Utilities.InDevMode() ? @"\\SOURCE2\Users\danF\Documents\Visual Studio 2017\Projects\AssemblyPrintout\AssemblyPrintout\data" : @"C:\inven";
        #region local documents
        public static string ReadExportData => $@"{LocalPath}\EXPORT.TXT";
        public static string WriteExportData => $@"{LocalPath}\_Export.txt";
        public static string Daily7Path => $@"{LocalPath}\Daily_7.txt";
        public static string AssemblySchedule => $@"{LocalPath}\Assembly_Schedule.txt";
        public static string QBError => $@"{SourceDir}\LOG\QBERROR.txt";
        public static string CSError => $@"{SourceDir}\LOG\csharpError.txt";
        public static string JobberError => $@"{SourceDir}\LOG\SALESLOG.LOG";
        //public static string prod2017Debug = @"C:\Users\Dan\Documents\Visual Studio 2017\Projects\AssemblyPrintout\AssemblyPrintout\data\total2017.csv";
        //public static string exportDataDev = @"C:\Users\Dan\Documents\Visual Studio 2017\Projects\AssemblyPrintout\AssemblyPrintout\data\EXPORT.TXT";
        //public static string assemblyDev = @"C:\Users\Dan\Documents\Visual Studio 2017\Projects\AssemblyPrintout\AssemblyPrintout\data\Assembly_Schedule.txt";
        //public static string d7Dev = @"C:\Users\Dan\Documents\Visual Studio 2017\Projects\AssemblyPrintout\AssemblyPrintout\data\Daily_7.txt";
        //public static string requiredDev = @"C:\Users\Dan\Documents\Visual Studio 2017\Projects\AssemblyPrintout\AssemblyPrintout\data\reqd.txt";
        //public static string init_pathDev = @"C:\Users\Dan\Documents\Visual Studio 2017\Projects\AssemblyPrintout\AssemblyPrintout\data\INITDATA.txt";
        #endregion local documents
        #region network documents
        public static string TempDataDir => $@"{SourceDir}\TEMPDATA";
        public static string Production => $@"{SourceDir}\PRODUCTS.BAK";
        public static string AssemblyData => $@"{SourceDir}\PDATA.TXT";
        public static string Required => $@"{TempDataDir}\reqd.txt";
        public static string InitPath => $@"{TempDataDir}\INITDATA.txt";
        public static string TotalBackOrdered => $@"{TempDataDir}\totalbacked.log";
        #endregion network documents
        #region program location
        public static string notepad = "Notepad.exe";
        #endregion
    }
}
