using System;
using Microsoft.Win32;
using System.Diagnostics;
using System.Collections.Generic;
using static AssemblyPrintout.datatypes;

namespace AssemblyPrintout
{
	class utils
	{
		public int getj30()
		{
			DateTime now = new DateTime( );
			now = DateTime.Now;
			int this_year = DateTime.Now.Year;
			int daysSinceJ30 = 0;
			if(DateTime.Now.Month <= 6)
			{
				this_year -= 1;
			}
			string j30 = this_year.ToString( ) + "-06-30T00:00:01-6:00";
			bool result = DateTime.TryParse(j30, out DateTime oldJ30);
			if(result)
			{
				TimeSpan ts = now.Subtract(oldJ30);
				daysSinceJ30 = ts.Days;
			}
			return daysSinceJ30;
		}
		public int getYear()
		{
			int this_year = DateTime.Now.Year;
			if(DateTime.Now.Month <= 6)
			{
				this_year -= 1;
			}
			return this_year;
		}
		public string getDailyAvg()
		{
			dailyAvgs2017 avgs = new dailyAvgs2017( );
			switch(DateTime.Now.Month)
			{
				case 1: return avgs._1;
				case 2: return avgs._2;
				case 3: return avgs._3;
				case 4: return avgs._4;
				case 5: return avgs._5;
				case 6: return avgs._6;
				case 7: return avgs._7;
				case 8: return avgs._8;
				case 9: return avgs._9;
				case 10: return avgs._10;
				case 11: return avgs._11;
				case 12: return avgs._12;
				default: return "";
			}
		}
		//public string getPath(string _switch)
		//{
		//	string path = @"";
		//	string exportName = "error.txt";
		//	switch (_switch)
		//	{
		//		case "assembly":
		//			exportName = "";
		//			path += exportName;
		//			return path;
		//		case "daily7":
		//			exportName = "";
		//			path += exportName;
		//			return path;
		//		default:
		//			path += exportName;
		//			return path;
		//	}

			///to allow for multiple iterations of a file
			//int count = 0;
			//while (File.Exists(path))
			//{
			//    exportName = today[2] + "-" + today[0] + "-" + today[1] + "_AssemblySchedule" + count;
			//    path = @"C:\INVEN\" + exportName;
			//    count++;
			//}
		//}
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
			else
			{
				dateTime = DateTime.Now;
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
		public assemblyTimes getProductAssm(List<string> rawProducts)
		{
			assemblyTimes assemblyTimes = new assemblyTimes(); ;
			assemblyTimes.dict = new Dictionary<string, decimal>();
			foreach (string line in rawProducts)
			{
				string[] raw = line.Split(',');
				if (!String.IsNullOrEmpty(raw[0].Trim()))
				{
					if (decimal.TryParse(raw[1], out decimal assemblyTime))
					{
						assemblyTimes.dict.Add(raw[0].Trim(), assemblyTime);
					}
				}
			}
			return assemblyTimes;
		}
	}
}
