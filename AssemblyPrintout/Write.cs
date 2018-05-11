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
                            sw.WriteLine(" _" + p._pcode + "_________________________________________________________________________________________________________________________________________");
                            sw.WriteLine("|_______|______Description_______|_Years_Use_|_On_Hand_|_Days_Supply_|_Do_Not_Exceed_|_Low_Part_#_|__Needed__|_Hours_to_make_30-day_Supply_____|");
                            if (p.productList != null)
                            {
                                foreach (product _p in p.productList)
                                {
                                    sw.WriteLine("| " + _p._product + " | " + (_p.desc + _S).Remove(22) + " | " + (_p.yu + _S).Remove(9) + " | " + (_p.oh + _S).Remove(7) + " | " + (_p.ds + _S).Remove(11) + " | " + (_p.doNotExceed + _S).Remove(13) + " | " + (_p.lowPart._part + _S).Remove(10) + " | " + (_p.need + _S).Remove(8) + " | " + (_p.days30 + _S).Remove(6) + " | " + "                       |");
                                }
                            }
                            else { sw.WriteLine("|         NO PRODUCTS TO DISPLAY                                                                                                               |"); }
                            sw.WriteLine("|_" + p._pcode + "_______" + _L + "Total_|_" + (p.totalNeeded + _L).Remove(8) + "_|_" + (p.days30 + _L).Remove(6) + "_|_" + (p.hoursAssembled + _L).Remove(6) + "_Hours_Assembled_|");
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
