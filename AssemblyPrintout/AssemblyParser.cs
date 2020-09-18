using System;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using static AssemblyPrintout.Datatypes;

namespace AssemblyPrintout
{
    static class AssemblyParser
    {
        #region globalVarsAndConst
        static string _ = Environment.NewLine;
        #endregion globalVars
        #region lineParserSwitch
        public static DatasetRAW DoWork(List<string> dataset, double YesterdaysHours)
        {
            DatasetRAW pcodes = new DatasetRAW();
            Daily7Data daily7Data = new Daily7Data();
            neededAndAnnual naa = Utilities.GetAnnualUseHours();
            string lline = "";
            var ProductCodeDictionary = Utilities.Products.GroupBy(x => x.ProductCode).ToDictionary(x => x.Key, x => x.ToList());
            try
            {
                foreach (string line in dataset)
                {
                    switch (line[0])
                    {
                        case '╝':
                            //a code line
                            var code = new ProductCode(line);
                            if (ProductCodeDictionary.ContainsKey(code.Productcode)) pcodes.ProductCodes.Add(code.InitCode(ProductCodeDictionary[code.Productcode]));
                            continue;
                        case '└':
                            //beginning of daily_7 data, parts list
                            pcodes.daily7Data = daily7Parser(line);
                            pcodes.YesterdaysProductionHours = YesterdaysHours;
                            continue;
                        default:
                            continue;
                    }
                }
            }
            catch (Exception e)
            {
                Write.ErrorWriter(lline + _ + "Stopped at parser switch." + _ + e.InnerException + _ + " " + e.Message + _ + e.StackTrace); Environment.Exit(0);
            }

            try
            {
                pcodes.AnnualAssemblyHours = naa.AnnualHours;
                pcodes.AssembledHours = Math.Round(pcodes.ProductCodes.Sum(x => x.HoursAssembled), 2, MidpointRounding.AwayFromZero);
                pcodes.XdaysSupply = Math.Round(pcodes.ProductCodes.Sum(x => x.XdaysSupply), 1, MidpointRounding.AwayFromZero);
                if (pcodes.ProductCodes.Any())
                {
                    pcodes.ProdSurplusHr30 = Math.Round((pcodes.AssembledHours - ((pcodes.AnnualAssemblyHours / 250) * 30)), 2, MidpointRounding.AwayFromZero);
                    pcodes.ProdSurplusHr60 = Math.Round((pcodes.AssembledHours - ((pcodes.AnnualAssemblyHours / 250) * 60)), 2, MidpointRounding.AwayFromZero);
                    pcodes.ProdSurplusHr90 = Math.Round((pcodes.AssembledHours - ((pcodes.AnnualAssemblyHours / 250) * 90)), 2, MidpointRounding.AwayFromZero);
                    pcodes.ProdHrNeedThirty = naa.Needed30;
                    pcodes.ProdHrNeedSixty = naa.Needed60;
                }
                else
                {
                    pcodes.ProdSurplusHr30 = 0;
                    pcodes.ProdSurplusHr60 = 0;
                    pcodes.ProdSurplusHr90 = 0;
                    pcodes.ProdHrNeedThirty = 0;
                    pcodes.ProdHrNeedSixty = 0;
                    pcodes.ProdHrNeedNinety = 0;
                }
            }
            catch (Exception e)
            {
                Write.ErrorWriter("Stopped after parser switch." + _ + e.InnerException + _ + " " + e.Message + _ + e.StackTrace); Environment.Exit(0);
            }
            //pcodes.pcodes = pcodes.pcodes.OrderBy(x => x._pcode).ToList();
            return pcodes;
        }
        #endregion lineParserSwitch
        #region dataParser
        //public static ProductCode parsePCode(ProductCode InCode, string CodeRAW)
        //{
        //    List<string> RAWpcode = CodeRAW.Split('╥').ToList()[1].Split(',').ToList();
        //    if (double.TryParse(RAWpcode[0].Trim(), Style, Culture, out double o2)) InCode.HoursAssembled = Math.Round(o2, 2, MidpointRounding.AwayFromZero);
        //    if (int.TryParse(RAWpcode[1].Trim(), out int o3)) InCode.DayLimit = o3;
        //    return InCode;
        //}

        //public static ProductSubModel parseProductsAndParts(string ProductRAW, int dayLimit)
        //{
        //    string[] productDataRAW = ProductRAW.Split('╘').Last().Split('╒');
        //    List<string> numbers = productDataRAW.Last().Split(',').ToList();
        //    List<string> product = productDataRAW.First().Split('╙').ToList();
        //    List<string> a_product = product[0].Split(',').ToList();
        //    Prod = new ProductSubModel();
        //    Prod.lowParts = new List<PartSubModel>();
        //    Prod._product = a_product[0].Trim();
        //    Prod.desc = a_product[1].Trim();
        //    bool _rr0 = decimal.TryParse(a_product[2].Trim(), out decimal p0);
        //    bool _rr1 = decimal.TryParse(a_product[3].Trim(), out decimal p1);
        //    bool _rr2 = decimal.TryParse(a_product[4].Trim(), out decimal p2);
        //    bool _rr3 = int.TryParse(a_product[5].Trim(), out int p3);
        //    if (_rr0) { Prod.yu = Math.Round(p0, 2, MidpointRounding.AwayFromZero); }
        //    if (_rr1) { Prod.oh = Math.Round(p1, 2, MidpointRounding.AwayFromZero); }
        //    if (_rr2) { Prod.ds = Math.Round(p2, 2, MidpointRounding.AwayFromZero); }
        //    if (_rr3) { Prod.ytdSales = p3; }
        //    if (j30 > 60)
        //    {
        //        decimal yuCalc = ((Prod.yu / 365) * dayLimit) - Prod.oh;
        //        decimal ytdCalc = ((Prod.ytdSales / j30) * dayLimit) - Prod.oh;
        //        if (yuCalc > ytdCalc) { Prod.Needed = yuCalc; } else { Prod.Needed = ytdCalc; }
        //    }
        //    else
        //    {
        //        Prod.Needed = (((Prod.yu / 365) * dayLimit) - Prod.oh);
        //    }
        //    Prod.Needed = Math.Round(Prod.Needed, 0, MidpointRounding.AwayFromZero);

        //    if (Prod.Needed > 0)
        //    {
        //        if (decimal.TryParse(numbers[1].Trim(), Style, Culture, out decimal assemblyTime)) { Prod.AssemblyTime = assemblyTime; }
        //        Prod.NeededAssemblyHours = ((Prod.Needed * Prod.AssemblyTime) / 3600);
        //        if (Prod.NeededAssemblyHours < 1) { Prod.NeededAssemblyHours = Math.Round(Prod.NeededAssemblyHours, 3, MidpointRounding.AwayFromZero); }
        //        else { Prod.NeededAssemblyHours = Math.Round(Prod.NeededAssemblyHours, 2, MidpointRounding.AwayFromZero); }
        //        product.RemoveAt(0);
        //        partList = new List<PartSubModel>();
        //        if (product.Any())
        //        {
        //            foreach (string PartRaw in product)
        //            {
        //                string[] PartRawData = PartRaw.Split(',');
        //                if (int.TryParse(PartRawData[0], out int PartNumber) && PartDictionary.ContainsKey(PartNumber))
        //                {
        //                    var tempPart = new PartSubModel(PartDictionary[PartNumber]);
        //                    if (tempPart.Part.QuantityOnHand == 0 || tempPart.Part.YearsUse == 0) tempPart.ds = 0; else tempPart.ds = Math.Round(((decimal)tempPart.Part.QuantityOnHand / (decimal)tempPart.Part.YearsUse) * 365, 0, MidpointRounding.AwayFromZero);
        //                    if (!Utilities.PartPrefixFilter.Contains(tempPart.Part.PartTypePrefix) && !Utilities.PartNumberFilter.Contains(tempPart.PartNumber)) partList.Add(tempPart);
        //                }
        //            }
        //            if (partList.Any())
        //            {
        //                partList = partList.OrderBy(x => x.ds).ToList();
        //                Prod.lowParts.Add(partList[0]);
        //                Prod.doNotExceed = Math.Round(((Prod.yu / 365) * Prod.lowParts[0].ds) - Prod.oh, 0, MidpointRounding.AwayFromZero);
        //            }
        //            else
        //            {
        //                Prod.lowParts.Add(EmptyPart);
        //            }
        //        }
        //        else { Prod.lowParts.Add(EmptyPart); }
        //    }
        //    return Prod;
        //}
        //public static Dictionary<string, PartValues> GetPartData(List<string> dataset)
        //{
        //    Dictionary<string, PartValues> partpack_data = new Dictionary<string, PartValues>();
        //    for (int i = (dataset.Count - 1); i >= 0; i--)
        //    {
        //        PartValues p;
        //        string[] parts = dataset[i].Split('╧');
        //        foreach (string pr in parts)
        //        {
        //            p = new PartValues();
        //            if (pr.Length > 0)
        //            {
        //                string[] x = pr.Split(',');
        //                if (decimal.TryParse(x[1], out decimal YearsUSe)) p.YearsUse = YearsUSe;
        //                if (decimal.TryParse(x[2], out decimal QuantityOnHand)) p.OnHand = QuantityOnHand;
        //                if (decimal.TryParse(x[3], out decimal QuantityAllocated)) p.QuantityAllocated = QuantityAllocated;
        //                if (!partpack_data.ContainsKey(x[0])) partpack_data.Add(x[0], p);
        //            }
        //        }
        //    }
        //    return partpack_data;
        //}
        #endregion dataParser
        #region daily7parse
        public static Daily7Data daily7Parser(string line)
        {
            Daily7Data daily7Data = new Daily7Data();
            daily7Data.partNumbers = new List<string>();
            string[] data = line.Split('└');
            foreach (PartModel Part in Utilities.Parts.Where(x => !x.Vendor.Contains("LEE") && (x.GlobalCycleTime != 0 || x.SecondCycleTime != 0))) { daily7Data.partNumbers.Add(Part.PartNumber.ToString()); }
            string[] _data = data[1].Split(',');
            daily7Data.hoursForYearsSales = _data[0];
            daily7Data.prodHoursPerDay = _data[1];
            daily7Data.totalHours = _data[2];
            daily7Data.assembledHours = _data[3];
            daily7Data.hoursNeeded30 = _data[4];
            daily7Data.hoursNeeded60 = _data[6];
            daily7Data.hoursNeeded90 = _data[8];
            daily7Data.surplusHours30 = _data[5];
            daily7Data.surplusHours60 = _data[7];
            daily7Data.surplusHours90 = _data[9];
            return daily7Data;
        }
        #endregion daily7parse
        #region quicksort
        public static CheckPart Quicksort(List<string> Data)
        {
            CheckPart Parts = new CheckPart();
            CheckPartPart cp;
            foreach (string d in Data)
            {
                if (Data.IndexOf(d) == 0 && d.Contains("ð") && int.TryParse(d.Replace("ð", ""), out int DSPLY))
                {
                    Parts.DayLimit = DSPLY;
                }
                else
                {
                    cp = new CheckPartPart();
                    if (int.TryParse(d.Substring(0, 13), out int qty))
                    {
                        cp.Qty = qty;
                        cp.Part = d.Substring(14, 6);
                        cp.Desc = d.Substring(22, 20);
                        cp.Vend = d.Substring(45, d.Length - 45);
                        Parts.Parts.Add(cp);
                    }
                }
            }
            if (Parts.DayLimit == null) { Parts.DayLimit = 30; }
            Parts.Parts = Parts.Parts.OrderBy(x => x.Part).OrderBy(xx => xx.Vend).ToList();
            return Parts;
        }
        #endregion quicksort
        #region production hours parser
        public static string GetLastYearData(List<string> data)
        {
            foreach (string d in data)
            {
                string[] values = d.Split(',');
                if (values.Count() == 5)
                {
                    if (DateTime.TryParse(values[0], out DateTime date))
                    {
                        DateTime tempDate = date.AddYears(1);
                        if (DateTime.Now.Date <= tempDate.Date)
                        {
                            if (decimal.TryParse(values[4], out decimal daysSupply))
                            {
                                return daysSupply.ToString();
                            }
                        }
                    }
                }
            }
            return null;

        }


        public static ProductionDataPack GetTodaysProductionData(List<string> data)
        {
            ProductionDataPack pdp = new ProductionDataPack();
            for (int i = data.Count - 1; i >= 0; i--)
                if (data[i].Length >= 84 && DateTime.TryParse(data[i].Substring(74, 10), out DateTime pDate))
                    if (pDate.Date != DateTime.Now.Date)
                        break;
                    else if (int.TryParse(data[i].Substring(49, 6), out int produced) && int.TryParse(data[i].Substring(0, 5), out int ProductNumber) && Utilities.ProductDictionary.ContainsKey(ProductNumber))
                        pdp.Today.Add(new ProductionLine(produced, Utilities.ProductDictionary[ProductNumber].AssemblyTime));
            return pdp;
        }

        public static double CalculateProductionTime(ProductionDataPack productionData) => (double)(productionData.Today.Sum(pl => pl.AssemblyTime * pl.Produced) / 3600M);

        #endregion product hours parser
    }
    static class AssemblyOrganizer
    {
        public static List<ProductData> ParseIn(List<string> data)
        {
            List<ProductData> productDataList = new List<ProductData>();
            ProductData ProdData;
            data.ForEach(D =>
            {
                List<string> ALine;
                if ((ALine = D.Split('|').ToList()) != null)
                {
                    ProdData = new ProductData()
                    {
                        Data = ALine.Last().Split(',').ToList()
                    };
                    ALine.Remove(ALine.Last());
                    ALine.ForEach(_a =>
                    {
                        ProdData.Parts.Add(new PartData(_a.Split(',')));
                    });
                    productDataList.Add(ProdData);
                }
            });
            return productDataList;
        }

        public static List<Entry> Pack(List<ProductData> data)
        {
            List<Entry> entries = new List<Entry>();
            Entry E;

            foreach (ProductData d in data)
            {
                E = new Entry();
                if (!string.IsNullOrEmpty(d.Data[0].Trim())) { E.ProductNum = d.Data[0].Trim(); } else { E.ProductNum = ""; }
                if (!string.IsNullOrEmpty(d.Data[1])) { E.Description = d.Data[1]; } else { E.Description = ""; }
                if (!string.IsNullOrEmpty(d.Data[2].Trim())) { E.ProductCode = d.Data[2].Trim(); } else { E.ProductCode = ""; }
                if (!string.IsNullOrEmpty(d.Data[3].Trim())) { E.AssemblyTime = d.Data[3].Trim(); } else { E.AssemblyTime = ""; }
                if (!string.IsNullOrEmpty(d.Data[4].Trim())) { E.Qty = d.Data[4].Trim(); } else { E.Qty = ""; }
                if (d.Parts != null && d.Parts.Count > 0)
                {
                    E.Parts = new List<Part>();
                    Part p = new Part();
                    foreach (PartData pd in d.Parts)
                    {
                        p = new Part();
                        if (!string.IsNullOrEmpty(pd.Data[0].Trim())) p.PartId = pd.Data[0].Trim(); else p.PartId = "";
                        if (!string.IsNullOrEmpty(pd.Data[1].Trim())) p.Description = pd.Data[1].Trim(); else p.Description = "";
                        if (!string.IsNullOrEmpty(pd.Data[2].Trim())) p.Vendor = pd.Data[2].Trim(); else p.Vendor = "";
                        E.Parts.Add(p);
                    }
                }
                entries.Add(E);
            }

            return entries;
        }

        public static List<IGrouping<string, Entry>> Sort(List<Entry> entries)
        {
            List<List<Entry>> entryPacks = new List<List<Entry>>();
            entries = entries.OrderBy(x => x.ProductCode).ToList();
            List<IGrouping<string, Entry>> ee = entries.GroupBy(x => x.ProductCode).ToList();
            foreach (IGrouping<string, Entry> e in ee)
            {
                e.OrderBy(x => x.ProductNum);
            }
            return ee;
        }
    }
}
