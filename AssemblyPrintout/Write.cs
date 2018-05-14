using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using static AssemblyPrintout.datatypes;
using System.Diagnostics;

namespace AssemblyPrintout
{
    class Write
    {
        utils u = new utils();
        string dir = "C:\\INVEN\\_EXPORT.txt";
        string _S = "                                                                                     ";
        string _L = "_______________________________________________________________________________";
        public void Writer(datasetRAW dsr)
        {
            string top = u.getj30() + " Days since June 30th, " + (DateTime.Now.Year - 1);
            if (File.Exists(dir))
            {
                try
                {
                    using (StreamWriter sw = new StreamWriter(dir))
                    {
                        sw.WriteLine(top);
                        foreach (pcode p in dsr.pcodes)
                        {
                            sw.WriteLine("These Items have less than " + p.dayLimit + " days supply.");
                            sw.WriteLine(" _" + p._pcode + "_______________________________________________________________________________________________");
                            sw.WriteLine("|Product|________________________|_Years_|___On__|_Days___|_Do_Not_|__Low___|________|_Hours_for_____|");
                            sw.WriteLine("|__No.__|______Description_______|__Use__|__Hand_|_Supply_|_Exceed_|_Part #_|_Needed_|_30-Day Supply_|");
                            if (p.productList != null)
                            {
                                foreach (product _p in p.productList)
                                {
                                    sw.WriteLine("| " + _p._product + " | " + (_p.desc + _S).Remove(22) + " | " + (_S).Remove(5 - _p.yu.ToString().Length) + _p.yu + " | " + (_S).Remove(5 - _p.oh.ToString().Length) + _p.oh + " | " + (_S).Remove(6 - _p.ds.ToString().Length) + _p.ds + " | " + (_S).Remove(6 - _p.doNotExceed.ToString().Length) + _p.doNotExceed + " | " + (_S).Remove(6 - _p.lowParts[0]._part.Length) + _p.lowParts[0]._part + " | " + (_S).Remove(6 - _p.need.ToString().Length) + _p.need + " | " + (_p.days30 + _S).Remove(6) + " | ");
                                }
                            }
                            else { sw.WriteLine(("|         NO PRODUCTS TO DISPLAY                                                                                                               ").Remove(94)+"|"); }
                            sw.WriteLine("|_" + p._pcode + (_L).Remove(37) + "Total_|" + (_L).Remove(7 - p.totalNeeded.ToString().Length) + p.totalNeeded + "_|_" + (_L).Remove(6 - p.days30.ToString().Length) + p.days30 + "_|_" + (p.hoursAssembled + _L).Remove(6) + "_Hours_Assembled___|");
                            sw.WriteLine();
                        }
                    }
                    Process.Start("C:\\Program Files\\Sublime Text 3\\sublime_text.exe", "C:\\INVEN\\_EXPORT.txt");
                }
                catch (Exception e) { Environment.Exit(0); }
            }
        }

        public void ErrorWriter(string error)
        {
            if (File.Exists(dir))
            {
                try { using (StreamWriter sw = new StreamWriter(dir)) { sw.WriteLine(error); } }
                catch (Exception e) { Environment.Exit(0); }
            }
        }
    }
}
