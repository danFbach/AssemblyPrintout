using System;
using System.Linq;
using System.IO;

namespace AssemblyPrintout
{
    class utils
    {
        public int getj30()
        {
            DateTime now = new DateTime();
            now = DateTime.Now;            
            int this_year = DateTime.Now.Year;
            int daysSinceJ30 = 0;
            if(DateTime.Now.Month <= 6)
            {
                if(DateTime.Now.Day < 30)
                {
                    this_year -= 1;
                }
            }
            string j30 = this_year.ToString() + "-06-30T00:00:01-6:00";
            bool result = DateTime.TryParse(j30, out DateTime oldJ30);
            if (result)
            {
                TimeSpan ts = now.Subtract(oldJ30);
                daysSinceJ30 = ts.Days;
            }
            return daysSinceJ30;
        }
        public string getFilename()
        {
            string[] today = DateTime.Now.Date.ToString().Split(' ').First().Split('/');
            string exportName = today[2] + "-" + today[0] + "-" + today[1] + "_AssemblySchedule";
            string path = @"C:\INVEN\" + exportName;
            int count = 0;
            while (File.Exists(path))
            {
                exportName = today[2] + "-" + today[0] + "-" + today[1] + "_AssemblySchedule" + count;
                path = @"C:\INVEN\" + exportName;
                count++;
            }
            return path;
        }
        //public void openPDF(string path)
        //{
        //    path += ".pdf";
        //    GetAdobeLocation(path);

        //    //path += ".oxps";
        //    //ProcessStartInfo psi = new ProcessStartInfo();
        //    //psi.Arguments = path;
        //    //psi.FileName = @"C:\Windows\system32\xpsrchvw.exe";
        //    //Process process = new Process();
        //    //process.StartInfo = psi;
        //    //process.Start();
        //}
        //private void GetAdobeLocation(string filename)
        //{
        //    var hkeyLocalMachine = Registry.LocalMachine.OpenSubKey(@"Software\Classes\Software\Adobe\Acrobat");
        //    if (hkeyLocalMachine != null)
        //    {
        //        var exe = hkeyLocalMachine.OpenSubKey("Exe");
        //        if (exe != null)
        //        {
        //            var acrobatPath = exe.GetValue(null).ToString();

        //            if (!string.IsNullOrEmpty(acrobatPath))
        //            {
        //                var process = new Process
        //                {
        //                    StartInfo =
        //            {
        //                UseShellExecute = false,
        //                FileName = acrobatPath,
        //                Arguments = filename
        //            }
        //                };

        //                process.Start();
        //            }
        //        }
        //    }
        //}
    }
}
