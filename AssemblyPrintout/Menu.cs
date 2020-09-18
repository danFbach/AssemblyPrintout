using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using static AssemblyPrintout.Datatypes;

namespace AssemblyPrintout
{
    class Menu
    {
        public static void ParseArgs(string[] args)
        {
            Utilities.UpdateAvgs(false);
            try
            {
                switch (args[0])
                {
                    case "-x":
                        Utilities.UpdateAvgs(true);
                        break;
                    case "-bkosort":
                        Write.BkoWriter(AssemblyOrganizer.Sort(AssemblyOrganizer.Pack(AssemblyOrganizer.ParseIn(Read.GenericRead(@"C:\INVEN\EXPORT.txt").ToList()))));
                        Process.Start("notepad.exe", @"C:\INVEN\_EXPORT.txt");
                        Environment.Exit(0);
                        break;
                    case "-f":
                        JobberParser.DumpData(JobberParser.GetInvoiceData(JobberParser.ParseBackOrders()));
                        Environment.Exit(0);
                        break;
                    case "-bkod":
                        if (args.Length != 2) Utilities.ExceptionExit("An invalid number of arguments were supplied by the calling program.");
                        if (args[1].Length == 8)
                        {
                            string[] DateComponents = args[1].Split('-'); //Break up date string to parse into DateTime Type
                            if (DateTime.TryParse($"{DateComponents[0]}/{DateComponents[1]}/{(20 + DateComponents[2])}", out DateTime DateLim))
                                JobberParser.DumpData(JobberParser.GetInvoiceData(JobberParser.ParseBackOrders()), DateLim);
                            else
                                Utilities.ExceptionExit("Date format was invalid and not parseable.", null, true);
                        }
                        Environment.Exit(0);
                        break;
                    case "-bko":
                        string CustomerCode = (args.Length >= 2 && args[1] != "_BLANK") ? args[1] : string.Empty;
                        string ReportType = (args.Length == 3) ? args[2] : string.Empty;
                        JobberParser.DumpData(
                            JobberParser.GetInvoiceData(
                                JobberParser.ParseBackOrders()), ReportType, CustomerCode, (args.Length > 1));
                        Environment.Exit(0);
                        break;
                    case "-a":
                        if (File.Exists(PathVars.ReadExportData))
                        {
                            ///GETS DATA FOR NEW ASSEMBLY SCHEDULE AND DAILY_7 PRINTOUT
                            List<string> data0 = Read.ExportReader(PathVars.ReadExportData);
                            //data = r.reader(path.exportDataLocal);
                            ///PARSES GATHERED DATA, IF THERE IS DATA (I.E. THERE WASN'T A PROBLEM IN QB) IT WILL THEN PRINT AND OPEN NEW FILES | IF ALL ELSE FAILS, PRINT AN ERROR
                            if (data0.Any())
                            {
                                double[] CurrentProductionData = UpdateProductionMetrics();
                                DatasetRAW dsr = AssemblyParser.DoWork(data0, CurrentProductionData[1]);
                                Write.AssmPrintWriter(dsr, PathVars.AssemblySchedule, PathVars.Daily7Path, PathVars.Required);
                            }
                            else Write.ErrorWriter("There was an issue with the exported data from basic.");
                        }
                        else Write.ErrorWriter("Required Files were not found.");
                        Environment.Exit(0);
                        break;
                    case "-p":
                        UpdateProductionMetrics();
                        Environment.Exit(0);
                        break;
                    case "-q":
                        Write.QuickSortWriter(AssemblyParser.Quicksort(Read.GenericRead(PathVars.ReadExportData).ToList()), PathVars.WriteExportData);
                        Process.Start("notepad.exe", PathVars.WriteExportData);
                        Environment.Exit(0);
                        break;
                }
            }
            catch (Exception e)
            {
                Utilities.ExceptionExit(string.Empty, e);
            }
        }

        public static double[] UpdateProductionMetrics()
        {
            List<string> data1 = Read.GenericRead(PathVars.Production).ToList(); ///GETS THIS MONTHS PRODUCTION DATA
            if (data1.Any() && Utilities.Products.Any()) ///IF WE HAVE PRODUCTION DATA AND ASSMEBLY TIMES, PROCEED
            {
                ///PUTS ASSEMBLY TIMES AND NO. PRODUCTS INTO NEAT CONTAINERS
                ///GET ACTUAL PRODUCTION TIME TOTALS FROM ((no.PRODUCT * ASSEMBLYtIME) / 3600)
                string[] TempCurrentProductionData = Read.GenericRead(PathVars.InitPath).ToArray();
                double[] CurrentProductionData = new double[] { 0, 0, 0, 0 };
                if (CurrentProductionData.Length < 5)
                {
                    for (int i = TempCurrentProductionData.Length; i < 4; i++)
                        CurrentProductionData[i] = TempCurrentProductionData.Length > i && double.TryParse(TempCurrentProductionData[i], out double CPDVal) ? CPDVal : 0;
                }
                CurrentProductionData[1] = Utilities.GetHoursAlt(CurrentProductionData);
                CurrentProductionData[0] = AssemblyParser.CalculateProductionTime(AssemblyParser.GetTodaysProductionData(data1));
                CurrentProductionData[2] = Utilities.GetDailyAvgForMonth(DateTime.Now.Date.Month);
                CurrentProductionData[3] = Utilities.HoursNeededPerDayForYearsSupply;
                using (StreamWriter sw = new StreamWriter(PathVars.InitPath))
                    foreach (double item in CurrentProductionData)
                        sw.WriteLine(item.ToString("####.##"));

                return CurrentProductionData;
            }
            return new double[] { 0, 0, 0, 0 };
        }
    }
}
