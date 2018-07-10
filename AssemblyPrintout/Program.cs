using System.Diagnostics;
using System.Collections.Generic;
using static AssemblyPrintout.datatypes;
using System;

namespace AssemblyPrintout
{
	class Program
	{
		static void Main(string[] args)
		{
			Read r = new Read( );
			Parser p = new Parser( );
			utils u = new utils( );
			Write w = new Write( );
			path path = new path( );
			datasetRAW dsr;
			assemblyTimes assemblyTimes;
			List<string> productData = new List<string>( );
			List<string> data = new List<string>( );
			string[] _args = { "-p" };
			foreach(string arg in args)
			{
				switch(arg)
				{
					case "-a":
						data = r.reader(path.exportData);
						List<string> hrs = new List<string>(r.genericRead(path.yestPrdctn));
						if(hrs.Count > 0 && data.Count > 0)
						{
							dsr = p._parser(data);
							w.customWriter(dsr, path.assembly, path.d7, hrs[0]);
						}
						else if(hrs.Count == 0 && data.Count > 0)
						{
							dsr = p._parser(data);
							w.customWriter(dsr, path.assembly, path.d7, "XXX.XX");
						}
						else
						{
							Process.Start(path.notepad, w.ErrorWriter("Required Files were not found"));
						}
						return;
					case "-p":
						data = r.genericRead(path.production);
						productData = r.genericRead(path.asmblyData);
						string avg = u.getDailyAvg();						
						if(data.Count > 0 && productData.Count > 0)
						{
							assemblyTimes = u.getProductAssm(productData);
							productionDataPack parsedProduction = p.GetPrdctnData(data, assemblyTimes);
							hours hours = p.calculateProductionTime(parsedProduction);
							w.genericLineWriter(hours.today.ToString( ), path.today);
							w.genericLineWriter(hours.yesterday.ToString( ), path.yesterday);
							w.genericLineWriter(avg, path.month);
						}
						return;
					case "xxx":
						//PARSE NEW AVG DATA FROM PRODUCTS.BAK
						//data = r.genericRead(path.totalProduction2017);
						//productData = r.genericRead(path.asmblyData);
						//assemblyTimes = u.getProductAssm(productData);
						//p.parseYear(data, assemblyTimes);						
						return;
				}
			}
		}
	}
}