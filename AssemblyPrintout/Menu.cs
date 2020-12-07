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
            Util.UpdateAvgs(false);
            try
            {
                switch (args[0])
                {
                    case "-lbl":
                        if (args.Count() == 2 && int.TryParse(args[1], out int LabelCount))
                        {
                            Write.PrintLabels(Read.GenericRead(Paths.ImportLabelData).ToList(), LabelCount);
                            Process.Start(Paths.ExportLabels);
                        }
                        break;
                    case "-x":
                        Util.UpdateAvgs(true);
                        break;
                    case "-bkosort":
                        Write.BkoWriter(AssemblyOrganizer.Sort(AssemblyOrganizer.Pack(AssemblyOrganizer.ParseIn(Read.GenericRead(Paths.ImportGenericData).ToList()))));
                        Process.Start("notepad.exe", Paths.ExportGenericData);
                        break;
                    case "-f":
                        JobberParser.DumpData();
                        break;
                    case "-SpecSched":
                        if (args.Length != 2) Util.ExceptionExit("An invalid number of arguments were supplied by the calling program.");
                        if (args[1].Length == 8)
                        {
                            var CodeData = Read.GenericRead(Paths.ImportTempImport).ToList();
                            if (Util.GetDateTimeFromArg(args[1], out DateTime DateLim))
                                JobberParser.DumpData(DateLim, CodeData);
                        }
                        break;
                    case "-mach":
                        if (args.Length != 2) Util.ExceptionExit("An invalid number of arguments were supplied by the calling program.");
                        if (args[1].Length == 8)
                        {
                            if (Util.GetDateTimeFromArg(args[1], out DateTime DateLim))
                                JobberParser.DumpData(DateLim);
                        }
                        break;
                    case "-bkod":
                        if (args.Length != 2) Util.ExceptionExit("An invalid number of arguments were supplied by the calling program.");
                        if (args[1].Length == 8)
                        {
                            if (Util.GetDateTimeFromArg(args[1], out DateTime DateLim))
                                JobberParser.DumpData(DateLim, null);
                            else Util.ExceptionExit("Date format was invalid and not parseable.", null, true);
                        }
                        break;
                    case "-bkop":
                        if (args.Length >= 2 && int.TryParse(args[1], out int ProductNumber))
                            JobberParser.DumpData(ProductNumber < 90001 ? ProductNumber : ProductNumber - 90000);
                        break;
                    case "-bko":
                        string CustomerCode0 = (args.Length >= 2 && args[1] != "_BLANK") ? args[1] : string.Empty;
                        string ReportType = (args.Length >= 3) ? args[2] : string.Empty;
                        JobberParser.DumpData(ReportType, CustomerCode0, args.Length > 1);
                        break;
                    case "-a":
                        if (File.Exists(Paths.ImportGenericData))
                        {
                            ///GETS DATA FOR NEW ASSEMBLY SCHEDULE AND DAILY_7 PRINTOUT
                            List<string> data0 = Read.ExportReader(Paths.ImportGenericData);
                            ///PARSES GATHERED DATA, IF THERE IS DATA (I.E. THERE WASN'T A PROBLEM IN QB) IT WILL THEN PRINT AND OPEN NEW FILES | IF ALL ELSE FAILS, PRINT AN ERROR
                            if (data0.Any())
                            {
                                double[] CurrentProductionData = UpdateProductionMetrics(false);
                                DatasetRAW dsr = AssemblyParser.DoWork(data0, CurrentProductionData[1]);
                                Write.AssmPrintWriter(dsr);
                            }
                            else Write.ErrorWriter("There was an issue with the exported data from basic.");
                        }
                        else Write.ErrorWriter("Required Files were not found.");
                        break;
                    case "-p":
                        UpdateProductionMetrics(true);
                        break;
                    case "-q":
                        Write.QuickSortWriter(AssemblyParser.Quicksort(Read.GenericRead(Paths.ImportGenericData).ToList()), Paths.ExportGenericData);
                        Process.Start("notepad.exe", Paths.ExportGenericData);
                        break;
                }
            }
            catch (Exception e)
            {
                Util.ExceptionExit(string.Empty, e);
            }
        }

        public static double[] UpdateProductionMetrics(bool ForceYesterday)
        {
            List<string> data1 = Read.GenericRead(Paths.ImportProduction).ToList(); ///GETS THIS MONTHS PRODUCTION DATA
            if (data1.Any() && Util.Products.Any()) ///IF WE HAVE PRODUCTION DATA AND ASSMEBLY TIMES, PROCEED
            {
                ///PUTS ASSEMBLY TIMES AND NO. PRODUCTS INTO NEAT CONTAINERS
                ///GET ACTUAL PRODUCTION TIME TOTALS FROM ((no.PRODUCT * ASSEMBLYtIME) / 3600)
                string[] TempCurrentProductionData = Read.GenericRead(Paths.ExportInitPath).ToArray();
                double[] CurrentProductionData = new double[] { 0, 0, 0, 0 };
                if (CurrentProductionData.Length < 5)
                {
                    for (int i = TempCurrentProductionData.Length; i < 4; i++)
                        CurrentProductionData[i] = TempCurrentProductionData.Length > i && double.TryParse(TempCurrentProductionData[i], out double CPDVal) ? CPDVal : 0;
                }
                CurrentProductionData[1] = Util.GetHoursAlt(CurrentProductionData, ForceYesterday);
                CurrentProductionData[0] = AssemblyParser.CalculateProductionTime(AssemblyParser.GetTodaysProductionData(data1));
                CurrentProductionData[2] = Util.GetDailyAvgForMonth(DateTime.Now.Date.Month);
                CurrentProductionData[3] = Util.HoursNeededPerDayForYearsSupply;
                using (StreamWriter sw = new StreamWriter(Paths.ExportInitPath))
                    foreach (double item in CurrentProductionData)
                        sw.WriteLine(item.ToString("###0.#0"));

                return CurrentProductionData;
            }
            return new double[] { 0, 0, 0, 0 };
        }
    }
}
