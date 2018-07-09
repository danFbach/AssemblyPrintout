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
			string[] _args = { "p" };
			foreach (string arg in args)
			{
				switch (arg)
				{
					case "-a":
						List<string> d = r.reader(@"C:\INVEN\EXPORT.txt");
						datasetRAW dsr = p._parser(d);
						w.customWriter(dsr, u.getPath("assembly"), u.getPath("daily7"));
						return;
					case "-p":
						//Console.Write("got to p");
						//Console.ReadKey();
						try
						{
							List<string> data = r.genericRead(@"\\SOURCE\INVEN\PRODUCTS.BAK");
							if (data.Count > 0)
							{
								List<string> productData = r.genericRead(@"\\SOURCE\INVEN\PDATA.TXT");
								//List<string> productData = r.genericRead(@"\\SOURCE\Inven\PRODUCTS.BAK");
								assemblyTimes assemblyTimes = u.getProductAssm(productData);
								List<productionLine> parsedProduction = p.GetPrdctnData(data, assemblyTimes);
								decimal ProdHours = p.calculateProductionTime(parsedProduction);
								w.genericLineWriter(ProdHours.ToString(), @"\\SOURCE\INVEN\todayhrs.txt");
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