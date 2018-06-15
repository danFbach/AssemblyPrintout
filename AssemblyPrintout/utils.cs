using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Win32;

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
        public string getPath(string _switch)
        {
            string path = @"C:\INVEN\";
            string exportName = "error.txt";
            switch (_switch)
            {
                case "assembly":
                    exportName = @"Assembly_Schedule.txt";
                    path += exportName;
                    return path;
                case "daily7":
                    exportName = @"Daily_7.txt";
                    path += exportName;
                    return path;
                default:
                    path += exportName;
                    return path;
            }

            ///to allow for multiple iterations of a file
            //int count = 0;
            //while (File.Exists(path))
            //{
            //    exportName = today[2] + "-" + today[0] + "-" + today[1] + "_AssemblySchedule" + count;
            //    path = @"C:\INVEN\" + exportName;
            //    count++;
            //}
        }
        public void openPDF(string path)
        {
            path += ".pdf";
            GetAdobeLocation(path);

            ///for using xps file as export
            //path += ".oxps";
            //ProcessStartInfo psi = new ProcessStartInfo();
            //psi.Arguments = path;
            //psi.FileName = @"C:\Windows\system32\xpsrchvw.exe";
            //Process process = new Process();
            //process.StartInfo = psi;
            //process.Start();
        }
        public DateTime GetToday()
        {
            DateTime dateTime = DateTime.MinValue;
            System.Net.HttpWebRequest request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create("http://www.microsoft.com");
            request.Method = "GET";
            request.Accept = "text/html, application/xhtml+xml, */*";
            request.UserAgent = "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.1; Trident/6.0)";
            request.ContentType = "application/x-www-form-urlencoded";
            request.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
            System.Net.HttpWebResponse response = (System.Net.HttpWebResponse)request.GetResponse();
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                string todaysDates = response.Headers["date"];

                dateTime = DateTime.ParseExact(todaysDates, "ddd, dd MMM yyyy HH:mm:ss 'GMT'",
                    System.Globalization.CultureInfo.InvariantCulture.DateTimeFormat, System.Globalization.DateTimeStyles.AssumeUniversal);
            }
            return dateTime;
        }
        private void GetAdobeLocation(string filename)
        {
            var hkeyLocalMachine = Registry.LocalMachine.OpenSubKey(@"Software\Classes\Software\Adobe\Acrobat");
            if (hkeyLocalMachine != null)
            {
                var exe = hkeyLocalMachine.OpenSubKey("Exe");
                if (exe != null)
                {
                    var acrobatPath = exe.GetValue(null).ToString();

                    if (!string.IsNullOrEmpty(acrobatPath))
                    {
                        var process = new Process
                        {
                            StartInfo =
                    {
                        UseShellExecute = false,
                        FileName = acrobatPath,
                        Arguments = filename
                    }
                        };

                        process.Start();
                    }
                }
            }
        }
    }
    //class dataMangement
    //{
    //    utils u = new utils();
    //    Write w = new Write();
    //    Read r = new Read();
    //    public void updateFiles()
    //    {
    //        string today = u.GetToday().ToString();
    //        string savedToday = r.reader(@"\\SOURCE\INVEN\TODAY.TXT").First();
    //        List<string> savedProdHist = r.reader(@"\\SOURCE\INVEN\PRODHIST.TXT");
    //        string lastYearData = r.reader(@"\\SOURCE\INVEN\LASTYEAR.TXT").First();
    //        string[] st = savedToday.Split('|');
    //        string todaysDate = st[1];
    //        string todaysNum = st[0];
    //        if (today != todaysDate)
    //        {
    //            string prodVal = lastYearData.Split('|')[1];
    //            foreach(string ph in savedProdHist)
    //            {
    //                string[] _ph = ph.Split('|');
    //                if(_ph[0] == todaysNum)
    //                {
    //                    string newph = (_ph[0].ToString() + prodVal);

    //                    savedProdHist[savedProdHist.IndexOf(ph)] = newph;
    //                    w.genericListWriter(savedProdHist.ToList(), @"\\SOURCE\INVEN\PRODHIST.TXT");
    //                    break;
    //                }
    //            }
    //        }
    //    }
    //}
}
