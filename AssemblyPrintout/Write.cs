using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using static AssemblyPrintout.Datatypes;

namespace AssemblyPrintout
{
    static class Write
    {
        static readonly string Br = Environment.NewLine;
        static string S => "                                                                                     ";
        static string L => "_______________________________________________________________________________";
        static readonly string Today = DateTime.Now.Date.ToString().Split(' ').First().Replace('/', '-');
        static PrintProduct PP = new PrintProduct();
        public static void AssmPrintWriter(DatasetRAW dsr)
        {
            int count = 0;
            if (File.Exists(Paths.ExportAssemblyData)) File.Delete(Paths.ExportAssemblyData);
            if (File.Exists(Paths.ExportDaily7)) File.Delete(Paths.ExportDaily7);
            string dailyhours = Math.Round((dsr.AnnualAssemblyHours / 250), 2, MidpointRounding.AwayFromZero).ToString();
            try
            {
                using (StreamWriter sw = new StreamWriter(Paths.ExportRequired)) sw.Write(dailyhours);
                using (StreamWriter sw = File.CreateText(Paths.ExportAssemblyData))
                {
                    Util.SourceOfflineWarning(sw);
                    Util.JobberOfflineWarning(sw);
                    sw.WriteLine($"Assembly Schedule for {DateTime.Now.ToShortDateString()}{Br}{Util.Getj30} Days since June 30th, {Util.GetFiscalYear}{Br}");
                    sw.WriteLine("* Low Part quantity 0");
                    foreach (ProductCode code in dsr.ProductCodes)
                    {
                        count += 1;
                        int count2 = 0;
                        if (code.Products != null && code.Products.Count > 0)
                        {
                            sw.WriteLine($"These Items have less than {code.DayLimit} days supply.");
                            sw.WriteLine($" _{code.Productcode}________________________________________________________________________________________");
                            sw.WriteLine($"|Product|________________________|_Years_|__On__|__Days__|_Do_Not_|__Low___|_For_{code.DayLimit}{(code.DayLimit.ToString().Length > 3 ? string.Empty : "___".Remove(3 - code.DayLimit.ToString().Length))}|_________|");
                            sw.WriteLine("|__No.__|______Description_______|__Use__|_Hand_|_Supply_|_Exceed_|_Part #_|Day_Sply|__Hours__|");
                            foreach (ProductModel prod in code.Products)
                            {
                                PP = new PrintProduct();
                                count2 += 1;
                                PP.Prod = Justify(prod.Number.ToString(), 0, S, 7, JustifyIs.center);
                                if (prod.ExtraData.LowPart != null) PP.Part = string.Format((prod.ExtraData.LowPart.Part.QuantityOnHand <= 0) ? "*{0}{1}*" : "{0}{1}", prod.ExtraData.LowPart.Part.PartTypePrefix, prod.ExtraData.LowPart.Part.PartNumber);
                                else PP.Part = "None";
                                PP.Part = Justify(PP.Part, 0, S, 8, JustifyIs.center);
                                PP.Desc = Justify(prod.Description, 0, S, 24, JustifyIs.left);
                                PP.YearsUse = Justify(prod.AnnualUse.ToString("######0"), 0, S, 7, JustifyIs.right);
                                PP.OnHand = Justify(prod.QuantityOnHand.ToString("#####0"), 0, S, 6, JustifyIs.right);
                                PP.DaysSupply = Justify(prod.ExtraData.DaysSupply.ToString("#######0"), 0, S, 8, JustifyIs.right);
                                PP.DoNotExceed = Justify(prod.ExtraData.DoNotExceed.ToString("#######0"), 0, S, 8, JustifyIs.right);
                                PP.Need = Justify(prod.ExtraData.Needed.ToString("#######0"), 0, S, 8, JustifyIs.right);
                                PP.Hour = Justify(prod.ExtraData.NeededAssemblyHours.ToString("####0.##0"), 0, S, 9, JustifyIs.right);
                                sw.WriteLine($"|{PP.Prod}|{PP.Desc}|{PP.YearsUse}|{PP.OnHand}|{PP.DaysSupply}|{PP.DoNotExceed}|{PP.Part}|{PP.Need}|{PP.Hour}|");
                            }
                            sw.WriteLine($"|_{code.Productcode}___________________________|Hours_Assembled:{Justify(code.HoursAssembled.ToString("####0.0"), 0, L, 7, JustifyIs.right)}|________|_Total:_|{Justify(code.TotalNeeded.ToString(), 0, L, 7, JustifyIs.right)}_|{Justify(code.XdaysSupply.ToString(), 1, L, 9, JustifyIs.right)}|");
                            sw.WriteLine();
                        }
                    }
                    sw.WriteLine($"Hours of Assembled Inventory: {dsr.AssembledHours}         Hours to Assemble Years Use: {dsr.AnnualAssemblyHours:####0.0}{Br}Hours to produce needed products for a {dsr.ProductCodes[0].DayLimit}-Day supply: {dsr.XdaysSupply}");
                    sw.WriteLine(Environment.NewLine + Environment.NewLine);
                    sw.WriteLine($"Yesterday's Production Hours: {dsr.YesterdaysProductionHours}");
                    //TODO: Repiar Get Daily avg function, as used below.
                    //sw.WriteLine("Last Year, This Month, Daily Avg: " + Utilities.GetDailyAvg());
                    sw.WriteLine($"Required Daily Hours to Produce a 1-year Supply: {dailyhours.Trim()}");
                }
                if (dsr.ProductCodes.Count > 1)
                {
                    using (StreamWriter sw = new StreamWriter(Paths.ExportDaily7))
                    {
                        Util.SourceOfflineWarning(sw);
                        Util.JobberOfflineWarning(sw);
                        sw.WriteLine($"THESE PARTS ARE NOT MADE BY US BUT HAVE CYCLE TIMES.{Br}");
                        foreach (string part in dsr.Daily7Data.PartNumbers) { sw.Write($"{part} "); }
                        sw.WriteLine($"{Br}{Br}{dsr.Daily7Data.HoursForYearsSales.Trim()} HOURS TO PRODUCE ALL PARTS FOR ESTIMATED SALES {Today}");
                        sw.WriteLine($"THERE MUST BE {dsr.Daily7Data.ProdHoursPerDay.Trim()} PRODUCTION HOURS PER DAY");
                        sw.WriteLine("THIS DOES NOT INCLUDE THE ASSEMBLY HOURS");
                        sw.WriteLine($"{dsr.Daily7Data.TotalHours.Trim()} TOTAL HOURS OF PARTS ON HAND {dsr.Daily7Data.AssembledHours.Trim()} ASSEMBLED.");
                        sw.WriteLine(Br + " ________________PARTS__________________       ________________PRODUCTS_______________");
                        sw.WriteLine("|_DAYS_|__HOURS_NEEDED__|_SURPLUS_HOURS_|     |_DAYS_|__HOURS_NEEDED__|_SURPLUS_HOURS_|");
                        sw.WriteLine($"| -30- | {Justify(dsr.Daily7Data.HoursNeeded30, S, 14, JustifyIs.right)} | {Justify(dsr.Daily7Data.SurplusHours30, S, 13, JustifyIs.right)} |     | -30- | {Justify(dsr.ProdHrNeedThirty.ToString(), S, 14, JustifyIs.right)} | {Justify(dsr.ProdSurplusHr30.ToString(), S, 13, JustifyIs.right)} |");
                        sw.WriteLine($"| -60- | {Justify(dsr.Daily7Data.HoursNeeded60, S, 14, JustifyIs.right)} | {Justify(dsr.Daily7Data.SurplusHours60, S, 13, JustifyIs.right)} |     | -60- | {Justify(dsr.ProdHrNeedSixty.ToString(), S, 14, JustifyIs.right)} | {Justify(dsr.ProdSurplusHr60.ToString(), S, 13, JustifyIs.right)} |");
                        sw.WriteLine($"| -90- | {Justify(dsr.Daily7Data.HoursNeeded90, S, 14, JustifyIs.right)} | {Justify(dsr.Daily7Data.SurplusHours90, S, 13, JustifyIs.right)} |     | -90- |                | {Justify(dsr.ProdSurplusHr90.ToString(), S, 13, JustifyIs.right)} |");
                        sw.WriteLine("|______|________________|_______________|     |______|________________|_______________|");
                        sw.WriteLine(" _______________________ _______________________");
                        sw.WriteLine("|   Daily Production    | Yesterdays Production |");
                        sw.WriteLine("|   Hours Required to   |         Hours         |");
                        sw.WriteLine("|Produce a 1-Year Supply|                       |");
                        sw.WriteLine($"| {Justify(dailyhours, S, 21, JustifyIs.center)} | {Justify(dsr.YesterdaysProductionHours.ToString("####.##"), S, 21, JustifyIs.center)} | ");
                        sw.WriteLine("|_______________________|_______________________|");
                        sw.WriteLine(Br + "SEE INVENTORY PRINTOUT FOR ITEMS THAT ARE IN SURPLUS" + Br);
                        sw.WriteLine($"Hours of Assembled Inventory: {dsr.AssembledHours}         Hours to Assemble Years Use: {dsr.AnnualAssemblyHours}{Br}Hours to produce needed products for a {dsr.ProductCodes[0].DayLimit}-Day supply: {dsr.XdaysSupply}");
                    }
                    if (File.Exists(Paths.ExportDaily7))
                    {
                        Process.Start("notepad.exe", Paths.ExportDaily7);
                        Thread.Sleep(100);
                    }
                }
                if (File.Exists(Paths.ExportAssemblyData))
                {
                    Process.Start("notepad.exe", Paths.ExportAssemblyData);
                }
            }
            catch (Exception e) { ErrorWriter("At Writer." + Environment.NewLine + e.Message + Br + e.StackTrace); Environment.Exit(0); }
        }
        public static void QuickSortWriter(CheckPart Parts, string Target)
        {
            if (File.Exists(Target)) { File.Delete(Target); }
            using (StreamWriter sw = new StreamWriter(Target))
            {
                sw.WriteLine(DateTime.Now.ToShortDateString());
                sw.WriteLine();
                sw.WriteLine($"LESS THAN {Parts.DayLimit} DAY{(Parts.DayLimit != 1 ? "S" : "")} SUPPLY ON HAND DON'T IGNORE THE SO ITEMS.");
                sw.WriteLine();
                Parts.Parts.ForEach(Part =>
                {
                    sw.WriteLine($" {Justify(Part.Qty.ToString(), 0, S, 12, JustifyIs.right)} {Justify(Part.Part, 0, S, 6, JustifyIs.left)} {Justify(Part.Desc, 0, S, 20, JustifyIs.left)} {Justify(Part.Vend, 0, S, 6, JustifyIs.left)}");
                });
            }
        }
        public static void BkoWriter(List<IGrouping<string, Entry>> data)
        {
            using (StreamWriter sw = new StreamWriter(Paths.ExportGenericData, false))
            {
                foreach (IGrouping<string, Entry> d in data)
                {
                    string _blank = " PR ID |       DESCRIPTION       | CODE | ASS.TM | QTY ";
                    sw.WriteLine(_blank);
                    foreach (Entry e in d)
                    {
                        if (e != null)
                        {
                            string temp = " " + (e.ProductNum + "         ").Remove(5) + " | " + (e.Description + "                         ").Remove(23) + " | " + (e.ProductCode + "     ").Remove(4) + " | " + (e.AssemblyTime + "      ").Remove(5) + "  | " + (e.Qty + "      ").Remove(5);
                            sw.WriteLine(temp);
                            if (e.Parts != null && e.Parts.Count > 0)
                            {
                                string blank = "    |*Part(s) on Backorder for " + (e.ProductNum + ":              ").Remove(16) + "  |";
                                sw.WriteLine(blank);
                                foreach (Part p in e.Parts)
                                {
                                    string partemp = "    | " + (p.PartId + "      ").Remove(6) + " | " + (p.Description + "                          ").Remove(23) + "  | " + (p.Vendor + "        ").Remove(6) + " |";
                                    sw.WriteLine(partemp);
                                }
                                sw.WriteLine("     --------------------------------------------");
                            }
                        }
                    }
                    sw.WriteLine("-------|-------------------------|------|--------|-----");
                }
                sw.Close();
            }
        }
        public static void GenericLineWriter(string _string, string OutputLocation, bool Append = false, bool DeleteExists = false)
        {
            try
            {
                if (DeleteExists && File.Exists(OutputLocation)) { File.Delete(OutputLocation); }
                using (StreamWriter sw = new StreamWriter(OutputLocation, Append))
                {
                    sw.WriteLine(_string);
                }
            }
            catch (Exception e)
            {
                ErrorWriter(DateTime.Now.ToShortDateString() + " - " + DateTime.Now.ToShortTimeString() + Br + "Stopped after parser switch." + Br + e.InnerException + Br + " " + e.Message + Br + e.StackTrace);
                Environment.Exit(0);
            }
        }
        public static void ErrorWriter(string error)
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(Paths.CSError, true)) { sw.WriteLine(DateTime.Now.ToShortDateString() + " - " + DateTime.Now.ToShortTimeString() + " - AssemblyPrintout v" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.Major.ToString() + " - " + Environment.GetEnvironmentVariable("COMPUTERNAME") + Environment.NewLine + error); }
                Environment.Exit(0);
            }
            catch { Environment.Exit(0); }
        }
        public static void PrintLabels(List<string> LabelData, int Count)
        {
            using (StreamWriter sw = new StreamWriter(Paths.ExportLabels))
            {
                for (int i = 0; i < Count - 1; i += 2)
                {
                    sw.WriteLine(LabelData[0] + S.Substring(0, 42 - LabelData[0].Length) + "│  " + LabelData[0]);
                    sw.WriteLine(LabelData[1] + S.Substring(0, 42 - LabelData[1].Length) + "│   " + LabelData[1]);
                    sw.WriteLine("Check if first shipment on PO    [  ]     │   Check if first shipment on PO    [  ]");
                    sw.WriteLine("Check if this order is complete  [  ]     │   Check if this order is complete  [  ]");
                    sw.WriteLine(LabelData[2] + S.Substring(0, 42 - LabelData[2].Length) + "│    " + LabelData[2]);
                    sw.WriteLine(S.Substring(0, 42) + "│");
                    sw.WriteLine("Quantity____________ Scrap___________     │   Quantity____________ Scrap___________");
                    sw.WriteLine("┌────────────────┐  ┌────────────────┐    │   ┌────────────────┐  ┌────────────────┐");
                    sw.WriteLine("│                │  │                │    │   │                │  │                │");
                    sw.WriteLine("│                │  │                │    │   │                │  │                │");
                    sw.WriteLine("└────────────────┘  └────────────────┘    │   └────────────────┘  └────────────────┘");
                    if (i == Count - 2 && Count % 2 == 0)
                        sw.WriteLine("──────────────────────────────────────────┴─────────────────────────────────────────");
                    else
                        sw.WriteLine("──────────────────────────────────────────┼─────────────────────────────────────────");


                }
                if (Count % 2 != 0)
                {
                    sw.WriteLine(LabelData[0] + S.Substring(0, 42 - LabelData[0].Length) + "│");
                    sw.WriteLine(LabelData[1] + S.Substring(0, 42 - LabelData[1].Length) + "│");
                    sw.WriteLine("Check if first shipment on PO    [  ]     │");
                    sw.WriteLine("Check if this order is complete  [  ]     │");
                    sw.WriteLine(LabelData[2] + S.Substring(0, 42 - LabelData[2].Length) + "│");
                    sw.WriteLine(S.Substring(0, 42) + "│");
                    sw.WriteLine("Quantity____________ Scrap___________     │");
                    sw.WriteLine("┌────────────────┐  ┌────────────────┐    │");
                    sw.WriteLine("│                │  │                │    │");
                    sw.WriteLine("│                │  │                │    │");
                    sw.WriteLine("└────────────────┘  └────────────────┘    │");
                }
            }
        }

        public static string Justify(string data, int check, string _space, int space, JustifyIs flag = JustifyIs.left)
        {
            int c = data.Split('.').Count();
            if (check == 1 && c > 1)
            {
                int zeros = (3 - data.Split('.')[1].Length);
                for (int i = 0; i < zeros; i++) data += "0";
            }
            else if (check == 1 && c == 1)
            {
                data += ".000";
            }
            int half = space / 2;
            int width = data.Length / 2;
            switch (flag)
            {
                case JustifyIs.left: return (space >= data.Length) ? (data + _space).Remove(space) : data.Remove(space - 3) + "...";
                case JustifyIs.right: return (space >= data.Length) ? _space.Remove(space - data.Length) + data : data.Remove(space - 3) + "...";
                case JustifyIs.center: return (space >= width) ? (_space.Remove(half - width) + data + _space).Remove(space) : data.Remove(space - 3) + "...";
                default: return data;
            }
        }
        public static string Justify(string data, string _space, int space, JustifyIs flag = JustifyIs.left)
        {
            data = data.Trim();
            int half = space / 2;
            int width = data.Length / 2;
            switch (flag)
            {
                case JustifyIs.left: return (space >= data.Length) ? (data + _space).Remove(space) : data.Remove(space - 3) + "...";
                case JustifyIs.right: return (space >= data.Length) ? _space.Remove(space - data.Length) + data : data.Remove(space - 3) + "...";
                case JustifyIs.center: return (space >= width) ? (_space.Remove(half - width) + data + _space).Remove(space) : data.Remove(space - 3) + "...";
                default: return data;
            }
        }
        public enum JustifyIs
        {
            left, right, center
        }

    }
}
