using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;

namespace AssemblyPrintout
{
    class Write
	{
		string _ = Environment.NewLine;
		utils u = new utils();
        string _S = "                                                                                     ";
        string _L = "_______________________________________________________________________________";
        string _D = "-------------------------------------------------------------------------------";
        string today = DateTime.Now.Date.ToString().Split(' ').First().Replace('/', '-');
        int count = 0;
        int count2 = 0;
        int a = 0;
        int b = 0;
        int d = 0;
        int c = 0;
        int g = 0;
        int e = 0;
        int f = 0;
        public void customWriter(datatypes.datasetRAW dsr, string assemblyPath, string daily7Path)
        {
			string dailyhours = Math.Round((dsr.annualAssemblyHours / 250), 2, MidpointRounding.AwayFromZero).ToString( );
			int j30 = u.getj30();
            string top = "Assembly Schedule for " + DateTime.Now.ToShortDateString() + _ + j30 + " Days since June 30th, " + u.getYear() + _;
            try
            {
                using (StreamWriter sw = File.CreateText(assemblyPath))
                {
                    //sw.WriteLine("For best results, set the font to \"Lucidia Console,\" Size 8.");
                    //sw.WriteLine(sw.NewLine);
                    sw.WriteLine(top);
                    foreach (datatypes.pcode code in dsr.pcodes)
                    {
                        count += 1;
                        count2 = 0;
                        if (code.productList != null && code.productList.Count > 0)
                        {
                            sw.WriteLine("These Items have less than " + code.dayLimit + " days supply.");
                            sw.WriteLine(" _" + code._pcode + "________________________________________________________________________________________");
                            sw.WriteLine("|Product|________________________|_Years_|__On__|__Days__|_Do_Not_|__Low___|________|_________|");
                            sw.WriteLine("|__No.__|______Description_______|__Use__|_Hand_|_Supply_|_Exceed_|_Part #_|_Needed_|__Hours__|");
                            foreach (datatypes.product prod in code.productList)
                            {
                                count2 += 1;
                                a = prod.yu.ToString().Length; 
                                b = prod.oh.ToString().Length; 
                                c = prod.ds.ToString().Length; 
                                d = prod.doNotExceed.ToString().Length; 
                                e = prod.lowParts[0]._part.Length;
                                f = prod.need.ToString().Length;
                                g = prod.xDayHours.ToString().Length; 
                                sw.WriteLine("| " + prod._product + " | " + (prod.desc + _S).Remove(22) + " |" + (_S).Remove(6 - a) + prod.yu + " |" + (_S).Remove(5 - b) + prod.oh + " |" + (_S).Remove(7 - c) + prod.ds + " |" + (_S).Remove(7 - d) + prod.doNotExceed + " |" + (_S).Remove(7 - e) + prod.lowParts[0]._part + " |" + (_S).Remove(7 - f) + prod.need + " |" + (_S).Remove(9 - g) + prod.xDayHours + "|");
                            }
                            sw.WriteLine("|_" + code._pcode + "___________________________|Hours_Assembled:" + (_L).Remove(7 - code.hoursAssembled.ToString().Length) + code.hoursAssembled + "|________|_Total:_|" + ((_L).Remove(4 - (code.totalNeeded.ToString().Length / 2)) + code.totalNeeded + _L).Remove(7) + "_|" + ((_L).Remove(4 - code.XdaysSupply.ToString().Length / 2) + code.XdaysSupply + _L).Remove(9) + "|");
                            sw.WriteLine();
                        }
                        else { continue; }

                    }
                    sw.WriteLine("Hours of Assembled Inventory: " + dsr.assembledHours + "         Hours to Assemble Years Use: " + dsr.annualAssemblyHours + _ + "Hours to produce needed products for a " + dsr.pcodes[0].dayLimit + "-Day supply: " + dsr.XdaysSupply);
					sw.WriteLine(Environment.NewLine + Environment.NewLine);
					sw.WriteLine("Yesterday's Production Hours: " + dsr.YesterdaysProductionHours);
					sw.WriteLine("Last Year, This Month, Daily Avg: " + u.getDailyAvg( ));
					sw.WriteLine("Required Daily Hours to Produce a 1-year Supply: " + dailyhours.Trim( ));
                }
				if (dsr.pcodes.Count > 1)
				{
					using (StreamWriter sw = File.CreateText(daily7Path))
					{
						sw.WriteLine("THESE PARTS ARE NOT MADE BY US BUT HAVE CYCLE TIMES." + _);
						foreach (string part in dsr.d7d.partNumbers) { sw.Write(part + " "); }
						sw.WriteLine(_ + _ + dsr.d7d.hoursForYearsSales.Trim() + " HOURS TO PRODUCE ALL PARTS FOR ESTIMATED SALES " + today);
						sw.WriteLine("THERE MUST BE " + dsr.d7d.prodHoursPerDay.Trim() + " PRODUCTION HOURS PER DAY");
						sw.WriteLine("THIS DOES NOT INCLUDE THE ASSEMBLY HOURS");
						sw.WriteLine(dsr.d7d.totalHours.Trim() + " TOTAL HOURS OF PARTS ON HAND " + dsr.d7d.assembledHours.Trim() + " ASSEMBLED.");
						sw.WriteLine(_ + " ________________PARTS__________________       ________________PRODUCTS_______________");
						sw.WriteLine("|_DAYS_|__HOURS_NEEDED__|_SURPLUS_HOURS_|     |_DAYS_|__HOURS_NEEDED__|_SURPLUS_HOURS_|");
						sw.WriteLine("| -30- |" + (_D.Remove(6 - (dsr.d7d.hoursNeeded30.Trim().Length / 2)) + " " + dsr.d7d.hoursNeeded30.Trim() + " " + _D).Remove(15) + " |" + (_D.Remove(5 - (dsr.d7d.surplusHours30.Trim().Length / 2)) + " " + dsr.d7d.surplusHours30.Trim() + " " + _D).Remove(14) + " |     | -30- |" + (_D.Remove(6 - (dsr.prodHrNeedthirty.ToString().Trim().Length / 2)) + " " + dsr.prodHrNeedthirty.ToString().Trim() + " " + _D).Remove(15) + " |" + (_S.Remove(5 - (dsr.prodSurplusHr30.ToString().Trim().Length / 2)) + " " + dsr.prodSurplusHr30.ToString().Trim() + " " + _D).Remove(15) + "|");
						sw.WriteLine("| -60- |" + (_D.Remove(6 - (dsr.d7d.hoursNeeded60.Trim().Length / 2)) + " " + dsr.d7d.hoursNeeded60.Trim() + " " + _D).Remove(15) + " |" + (_D.Remove(5 - (dsr.d7d.surplusHours60.Trim().Length / 2)) + " " + dsr.d7d.surplusHours60.Trim() + " " + _D).Remove(14) + " |     | -60- |" + (_D.Remove(6 - (dsr.prodHrNeedsixty.ToString().Trim().Length / 2)) + " " + dsr.prodHrNeedsixty.ToString().Trim() + " " + _D).Remove(15) + " |" + (_S.Remove(5 - (dsr.prodSurplusHr60.ToString().Trim().Length / 2)) + " " + dsr.prodSurplusHr60.ToString().Trim() + " " + _D).Remove(15) + "|");
						sw.WriteLine("| -90- |" + (_D.Remove(6 - (dsr.d7d.hoursNeeded90.Trim().Length / 2)) + " " + dsr.d7d.hoursNeeded90.Trim() + " " + _D).Remove(15) + " |" + (_D.Remove(5 - (dsr.d7d.surplusHours90.Trim().Length / 2)) + " " + dsr.d7d.surplusHours90.Trim() + " " + _D).Remove(14) + " |     | -90- |" + (_D.Remove(6 - (dsr.prodHrNeedninety.ToString().Trim().Length / 2)) + " " + dsr.prodHrNeedninety.ToString().Trim() + " " + _D).Remove(15) + " |" + (_S.Remove(5 - (dsr.prodSurplusHr90.ToString().Trim().Length / 2)) + " " + dsr.prodSurplusHr90.ToString().Trim() + " " + _D).Remove(15) + "|");
						sw.WriteLine("|______|________________|_______________|     |______|________________|_______________|");
						sw.WriteLine(" _______________________ _______________________");
						sw.WriteLine("|   Daily Production    | Yesterdays Production |");
						sw.WriteLine("|   Hours Required to   |         Hours         |");
						sw.WriteLine("|Produce a 1-Year Supply|                       |");
						sw.WriteLine("| " + (_D.Remove(10 - (dailyhours.Trim().Length / 2)) + dailyhours.Trim() + _D).Remove(21) + " | " + (_D.Remove(10 - (dsr.YesterdaysProductionHours.Trim().Length / 2)) + dsr.YesterdaysProductionHours.Trim() + _D).Remove(21) + " |");
						sw.WriteLine("|_______________________|_______________________|");

						sw.WriteLine(_ + "SEE INVENTORY PRINTOUT FOR ITEMS THAT ARE IN SURPLUS" + _);
						sw.WriteLine("Hours of Assembled Inventory: " + dsr.assembledHours + "         Hours to Assemble Years Use: " + dsr.annualAssemblyHours + _ + "Hours to produce needed products for a " + dsr.pcodes[0].dayLimit + "-Day supply: " + dsr.XdaysSupply);
					}
					Process.Start("Notepad.exe", daily7Path);
					Thread.Sleep(250);
				}
                Process.Start("Notepad.exe", assemblyPath);
            }
            catch (Exception e) { ErrorWriter(DateTime.Now.ToShortDateString() + " - " + DateTime.Now.ToShortTimeString() + _ + "Count 1: " + count.ToString() + " | Count 2: " + count2.ToString() + _ + " " + e.Message + _ + e.StackTrace); Environment.Exit(0); }
        }

        public void genericListWriter(List<string> strings, string OutputLocation)
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(OutputLocation))
                {
                    foreach (string s in strings)
                    {
                        sw.WriteLine(s);
                    }
                }
            }
            catch (Exception e)
            {
                ErrorWriter(DateTime.Now.ToShortDateString() + " - " + DateTime.Now.ToShortTimeString() + _ + "Stopped after parser switch." + _ + e.InnerException + _ + " " + e.Message + _ + e.StackTrace); Environment.Exit(0);
            }
        }
        public void genericLineWriter(string _string, string OutputLocation)
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(OutputLocation))
                {
                    sw.WriteLine(_string);
                }
            }
            catch (Exception e)
            {
                ErrorWriter(DateTime.Now.ToShortDateString() + " - " + DateTime.Now.ToShortTimeString() + _ + "Stopped after parser switch." + _ + e.InnerException + _ + " " + e.Message + _ + e.StackTrace); Environment.Exit(0);
            }
        }

        public string ErrorWriter(string error)
        {
            string erPath = @"C:\INVEN\csharpError.txt";
            try { using (StreamWriter sw = new StreamWriter(erPath)) { sw.WriteLine(error); } Process.Start("Notepad.exe", erPath); }
            catch { Environment.Exit(0); }
			return erPath;
        }
    }
}
