using System.Diagnostics;
using System.Collections.Generic;
using static AssemblyPrintout.datatypes;

namespace AssemblyPrintout
{
	class mainSwitch
	{
		public void _switch(string[] args)
		{
			Read r = new Read( );
			Parser p = new Parser( );
			utils u = new utils( );
			Write w = new Write( );
			paths path = new paths( );
			datasetRAW dsr;
			assemblyTimes assemblyTimes;
			List<string> data = new List<string>( );
			List<string> productData;
			string yesterdayHours;

			string monthlyAVG = u.getDailyAvg( ); ///PULL STORED MOTHLY AVG
			if(monthlyAVG != "0") { w.genericLineWriter(monthlyAVG, path.month); }

			yesterdayHours = u.getHoursAlt( ); ///GETS AND SETS YESTERDAY IF IT HAS NOT YET BEEN UPDATED TODAY // ALSO, IF NOTHING HAS BEEN ENTERED INTO PRODUCTION FOR TODAY YET, WILL RESET TODAY TO 0
			foreach(string arg in args)
			{
				switch(arg)
				{
					case "-a":
						data = r.reader(path.exportDataRemote); ///GETS DATA FOR NEW ASSEMBLY SCHEDULE AND DAILY_7 PRINTOUT

						///PARSES SAID DATA, IF THERE IS DATA (I.E. THERE WASN'T A PROBLEM IN QB) IT WILL THEN PRINT AND OPEN NEW FILES | IF ALL ELSE FAILS, PRINT AN ERROR
						if(data.Count > 0) { dsr = p._parser(data); dsr.yesterdayHours = yesterdayHours; w.customWriter(dsr, path.assembly, path.d7); }
						else { Process.Start(path.notepad, w.ErrorWriter("Required Files were not found")); }
						return;
					case "-p":
						productData = new List<string>(r.genericRead(path.asmblyData)); ///GETS ASSEMBLY TIMES FOR ALL PRODUCTS

						data = r.genericRead(path.production); ///GETS THIS MONTHS PRODUCTION DATA
												
						if(data.Count > 0 && productData.Count > 0) ///IF WE HAVE PRODUCTION DATA AND ASSMEBLY TIMES, PROCEED
						{
							///ORGANIZE ASSEMBLY TIMES
							assemblyTimes = u.getProductAssm(productData);

							///PUTS ASSEMBLY TIMES AND NO. PRODUCTS INTO NEAT CONTAINERS
							productionDataPack parsedProduction = p.GetPrdctnData(data, assemblyTimes);

							///GET ACTUAL PRODUCTION TIME TOTALS FROM ((no.PRODUCT * ASSEMBLYtIME) / 3600)
							hours hours = p.calculateProductionTime(parsedProduction);

							///IF THERE IS SOMETHING TO WRITE, WRITE IT TO ITS' DESIGNATED FILE
							w.genericLineWriter(hours.today.ToString( ), path.today);
						}
						return;
					case "-po":

						return;
						//case "xxx":
						//PARSE NEW AVG DATA FROM PRODUCTS.BAK
						//data = r.genericRead(path.totalProduction2017);
						//productData = r.genericRead(path.asmblyData);
						//assemblyTimes = u.getProductAssm(productData);
						//p.parseYear(data, assemblyTimes);						
						//return;
				}
			}
		}
	}
}
