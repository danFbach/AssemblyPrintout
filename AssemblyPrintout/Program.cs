using System;
using System.IO;
using static AssemblyPrintout.datatypes;
using System.Collections.Generic;
using System.Diagnostics;

namespace AssemblyPrintout
{
    class Program
    {
        static void Main(string[] args)
		{
			Read r = new Read();
			Parser p = new Parser();
			utils u = new utils();
			Write w = new Write();
			string[] CHANGE_ME = { "-p" };
			foreach (string arg in CHANGE_ME)
			{
				switch (arg)
				{
					case "-a":
						List<string> d = r.reader(@"C:\INVEN\EXPORT.txt");
						string hrs = r.genericRead(@"\\SOURCE\INVEN\TEMPDATA\YEST.TXT")[0];
						datasetRAW dsr = p._parser(d);
						w.customWriter(dsr, u.getPath("assembly"), u.getPath("daily7"), hrs);
						return;
					case "-p":
						try
						{
							List<string> data = r.genericRead(@"\\SOURCE\INVEN\PRODUCTS.BAK");
							if (data.Count > 0)
							{
								List<string> productData = r.genericRead(@"\\SOURCE\INVEN\PDATA.TXT");
								assemblyTimes assemblyTimes = u.getProductAssm(productData);
								productionDataPack parsedProduction = p.GetPrdctnData(data, assemblyTimes);
								hours hours = p.calculateProductionTime(parsedProduction);
								w.genericLineWriter(hours.today.ToString(), @"\\SOURCE\INVEN\TEMPDATA\TODAY.TXT");
								w.genericLineWriter(hours.yesterday.ToString( ), @"\\SOURCE\INVEN\TEMPDATA\YEST.TXT");
								w.genericLineWriter(hours.month.ToString( ), @"\\SOURCE\INVEN\TEMPDATA\MONTH.TXT");
							}
						}
						catch(Exception e)
						{
							string _ = Environment.NewLine;
							using(StreamWriter sw = new StreamWriter(@"C:\inven\csharpError.txt"))
							{
								sw.Write("Error." + _ + e.Message + _ + e.InnerException + e.StackTrace);
							}
						}
						return;
				}	
			}
		}
    }
}