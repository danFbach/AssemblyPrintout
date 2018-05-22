using System;
using System.IO;
using System.Diagnostics;
using System.Linq;

namespace AssemblyPrintout
{
    class Write
    {
        utils u = new utils();
        string _S = "                                                                                     ";
        string _L = "_______________________________________________________________________________";
        string today = DateTime.Now.Date.ToString().Split(' ').First().Replace('/', '-');
        int count = 0;
        int count2 = 0;
        public void Writer(datatypes.datasetRAW dsr, string assemblyPath, string daily7Path)
        {
            string top = "Assembly Schedule for " + today + Environment.NewLine + Environment.NewLine + u.getj30() + " Days since June 30th, " + (DateTime.Now.Year - 1);
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
                                //if (prod.lowParts.Count == 0)
                                //{
                                //    sw.WriteLine("| " + prod._product + " | " + (prod.desc + _S).Remove(22) + " | " + (_S).Remove(5 - prod.yu.ToString().Length) + prod.yu + " | " + (_S).Remove(4 - prod.oh.ToString().Length) + prod.oh + " | " + (_S).Remove(6 - prod.ds.ToString().Length) + prod.ds + " | " + (_S).Remove(6 - prod.doNotExceed.ToString().Length) + prod.doNotExceed + " |        | " + (_S).Remove(6 - prod.need.ToString().Length) + prod.need + " | " + (_S).Remove(7 - prod.XdaysSupply.ToString().Length) + prod.XdaysSupply + " |");
                                //}
                                //else
                                //{
                                    sw.WriteLine("| " + prod._product + " | " + (prod.desc + _S).Remove(22) + " |" + (_S).Remove(6 - prod.yu.ToString().Length) + prod.yu + " |" + (_S).Remove(5 - prod.oh.ToString().Length) + prod.oh + " |" + (_S).Remove(7 - prod.ds.ToString().Length) + prod.ds + " |" + (_S).Remove(7 - prod.doNotExceed.ToString().Length) + prod.doNotExceed + " |" + (_S).Remove(7 - prod.lowParts[0]._part.Length) + prod.lowParts[0]._part + " |" + (_S).Remove(7 - prod.need.ToString().Length) + prod.need + " |" + (_S).Remove(8 - prod.XdaysSupply.ToString().Length) + prod.XdaysSupply + " |");
                                //}
                            }
                            sw.WriteLine("|_" + code._pcode + "___________________________|Hours_Assembled:" + (_L).Remove(7 - code.hoursAssembled.ToString().Length) + code.hoursAssembled + "|________|_Total:_|" + (_L).Remove(7 - code.totalNeeded.ToString().Length) + code.totalNeeded + "_|" + (_L).Remove(8 - code.XdaysSupply.ToString().Length) + code.XdaysSupply + "_|");
                            sw.WriteLine();
                        }
                        else { continue; }

                    }
                    sw.WriteLine("Hours of Assembled Inventory: " + dsr.assembledHours + "        Hours to produce needed products for a " + dsr.pcodes[0].dayLimit + "-Day supply: " + dsr.XdaysSupply);
                }
                using (StreamWriter sw = File.CreateText(daily7Path))
                {
                    sw.WriteLine("THESE PARTS ARE NOT MADE BY US BUT HAVE CYCLE TIMES." + Environment.NewLine);
                    foreach (string part in dsr.daily7Data.partNumbers){ sw.Write(part + " "); }
                    sw.WriteLine(Environment.NewLine + Environment.NewLine + dsr.daily7Data.hoursForYearsSales + " HOURS TO PRODUCE ALL PARTS FOR ESTIMATED SALES " + today);
                    sw.WriteLine("THERE MUST BE " + dsr.daily7Data.prodHoursPerDay + " PRODUCTION HOURS PER DAY");
                    sw.WriteLine("THIS DOES NOT INCLUDE THE ASSEMBLY HOURS");
                    sw.WriteLine(dsr.daily7Data.totalHours.Trim() + " TOTAL HOURS OF PARTS ON HAND " + dsr.daily7Data.assembledHours + " ASSEMBLED.");
                    sw.WriteLine(Environment.NewLine + " DAYS |  HOURS NEEDED  | SURPLUS HOURS |");
                    sw.WriteLine("  30  |" + (_S).Remove(14 - dsr.daily7Data.hoursNeeded30.Length) + dsr.daily7Data.hoursNeeded30 + "  |" + (_S).Remove(14 - dsr.daily7Data.surplusHours30.Length) + dsr.daily7Data.surplusHours30 + " |");
                    sw.WriteLine("  60  |" + (_S).Remove(14 - dsr.daily7Data.hoursNeeded60.Length) + dsr.daily7Data.hoursNeeded60 + "  |" + (_S).Remove(14 - dsr.daily7Data.surplusHours60.Length) + dsr.daily7Data.surplusHours60 + " |");
                    sw.WriteLine("  30  |" + (_S).Remove(14 - dsr.daily7Data.hoursNeeded90.Length) + dsr.daily7Data.hoursNeeded90 + "  |" + (_S).Remove(14 - dsr.daily7Data.surplusHours90.Length) + dsr.daily7Data.surplusHours90 + " |");
                    sw.WriteLine(Environment.NewLine + "SEE INVENTORY PRINTOUT FOR ITEMS THAT ARE IN SURPLUS" + Environment.NewLine);
                    sw.WriteLine("Hours of Assembled Inventory: " + dsr.assembledHours + "        Hours to produce needed products for a " + dsr.pcodes[0].dayLimit + "-Day supply: " + dsr.XdaysSupply);
                }
                Process.Start("Notepad.exe", daily7Path);
                Process.Start("Notepad.exe", assemblyPath);
            }
            catch (Exception e) { ErrorWriter(count2 + count + " " + e.Message + Environment.NewLine + e.StackTrace); Environment.Exit(0); }
        }

        public void ErrorWriter(string error)
        {
            try { using (StreamWriter sw = new StreamWriter(@"C:\INVEN\csharpError.txt")) { sw.WriteLine(error); } }
            catch (Exception e) { Environment.Exit(0); }
        }
    }
}
