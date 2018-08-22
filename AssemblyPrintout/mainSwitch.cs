using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using static AssemblyPrintout.datatypes;
using System;

namespace AssemblyPrintout
{
	class mainSwitch
	{
			Read r = new Read( );
			paths path = new paths( );
			Parser p = new Parser( );
			Write w = new Write( );
		public void _switch(string[] args)
		{
			utils u = new utils( );
			datasetRAW dsr;
			assemblyTimes assemblyTimes = new assemblyTimes( );
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
						u.get2017data( );
						data = r.reader(path.exportDataRemote); ///GETS DATA FOR NEW ASSEMBLY SCHEDULE AND DAILY_7 PRINTOUT

						///PARSES SAID DATA, IF THERE IS DATA (I.E. THERE WASN'T A PROBLEM IN QB) IT WILL THEN PRINT AND OPEN NEW FILES | IF ALL ELSE FAILS, PRINT AN ERROR
						if(data.Count > 0) { dsr = p._parser(data); dsr.yesterdayHours = yesterdayHours; w.customWriter(dsr, path.assembly, path.d7); }
						else { w.ErrorWriter("Required Files were not found"); }
						return;
					case "-p":
						productData = new List<string>(r.genericRead(path.asmblyData)); ///GETS ASSEMBLY TIMES FOR ALL PRODUCTS
						if(productData.Count > 0)
						{
							assemblyTimes = u.getProductAssm(productData);
						}

						data = r.genericRead(path.production); ///GETS THIS MONTHS PRODUCTION DATA
						if(data.Count > 0 && assemblyTimes.dict.Count > 0) ///IF WE HAVE PRODUCTION DATA AND ASSMEBLY TIMES, PROCEED
						{
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
		public void get2017Data()
		{
			if(File.GetLastWriteTime(path.prod2017Data).Date < DateTime.Now.Date)
			{
				List<string> data = new List<string>( );
				data = r.genericRead(path.prod2017Remote); ///GETS 2017 DATA
				if(data.Count > 0)
				{
					string p2017 = p.get2017Data(data);///PARSES DATA TO FIND TODAYS VALUE, OR THE CLOSEST DATE TO TODAY
					if(p2017 != null)
					{
						w.genericLineWriter(p2017, path.prod2017Data);///PRINTS TO TXT FILE ON NETWORK DRIVE
					}
				}
			}
		}
	}
}
