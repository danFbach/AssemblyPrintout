using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AssemblyPrintout
{
    static class Paths
    {
        public static string SourceDir => !Util.InDevMode() && Util.SourceSalesIsOnline && Util.SourceInvenIsOnline ? SourceInven : DevInven;
        public static string SalesDir => !Util.InDevMode() && Util.SourceSalesIsOnline ? SourceSales : DevSales;
        public static string SourceSales = @"\\192.168.0.194\sales";
        public static string DevSales = @"\\192.168.0.94\sales";
        public static string SourceInven = @"\\192.168.0.194\Inven";
        public static string DevInven = @"\\192.168.0.94\inven";
        public static string LocalPath = Util.InDevMode() ? @"\\SOURCE2\Users\danF\Documents\Visual Studio 2017\Projects\AssemblyPrintout\AssemblyPrintout\data" : @"C:\inven";

        public static string ExportDaily7 => $@"{LocalPath}\Daily_7.txt";
        public static string ExportGenericData => $@"{LocalPath}\_Export.txt";
        public static string ExportBackorder => $@"{LocalPath}\BackOrderExport.txt";
        public static string ExportProductNeeded => $@"{LocalPath}\ProductsNeeded.txt";
        public static string ExportAssemblySchedule => $@"{LocalPath}\Assembly_Schedule.txt";
        public static string ImportGenericData => $@"{LocalPath}\EXPORT.TXT";
        public static string ExportAssemblyData => $@"{SourceDir}\PDATA.TXT";
        public static string ExportActualOnHand => $@"{TempDataDir}\ActualOnHand.tmp";
        public static string ExportBackorderVal => $@"{TempDataDir}\BackOrderVal.txt";
        public static string ExportBackorderValData => $@"{TempDataDir}\BackOrderValData.txt";
        public static string ImportPartDump(int Index) => $@"{TempDataDir}\PARTDUMP{Index}.TXT";
        public static string ImportProduction => $@"{SourceDir}\PRODUCTS.BAK";
        public static string QBError => $@"{SourceDir}\LOG\QBERROR.txt";
        public static string CSError => $@"{SourceDir}\LOG\csharpError.txt";
        public static string JobberError => $@"{SourceDir}\LOG\SALESLOG.LOG";
        public static string TempDataDir => $@"{SourceDir}\TEMPDATA";
        public static string ExportRequired => $@"{TempDataDir}\reqd.txt";
        public static string ExportInitPath => $@"{TempDataDir}\INITDATA.txt";
        public static string ExportTotalBackOrdered => $@"{TempDataDir}\totalbacked.log";
        #region program location
        public static string notepad = "Notepad.exe";
        #endregion
    }
}
