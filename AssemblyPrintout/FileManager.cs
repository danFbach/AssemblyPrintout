using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AssemblyPrintout
{
    static class FileManager
    {
        //public static IEnumerable<string> GetBkoFiles(string Source)
        //{
        //    if (!Directory.Exists(Source)) throw new Exception("Jobber Off", new Exception("Jobber PC is turned off. The enable the use of this program, the jobber computer MUST be ON and LOGGED into."));
        //    return Directory.GetFiles($@"{Source}\*.BKO");
        //}

        /// Generic File grabber, can filter by file extension if needed.
        public static IEnumerable<string> GetDirFiles(string Path, string FileExtension = "")
        {
            if (!Directory.Exists(Path)) throw new Exception("Jobber Off", new Exception("Jobber PC is turned off. The enable the use of this program, the jobber computer MUST be ON and LOGGED into."));
            return FileExtension == string.Empty ? Directory.GetFiles($@"{Path}") : Directory.GetFiles($@"{Path}\", $"*.{FileExtension}");
        }

        //public static List<string> GetInvoiceFiles(string Source)
        //{
        //    if (!Directory.Exists(Source)) throw new Exception("Jobber Off", new Exception("Jobber PC is turned off. The enable the use of this program, the jobber computer MUST be ON and LOGGED into."));
        //    return Directory.GetFiles(Source).ToList();
        //}
    }
}
