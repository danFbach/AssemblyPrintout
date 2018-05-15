using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace AssemblyPrintout
{
    class Parser
    {
        
        List<string> filter = new List<string> { "LB" , "MR", "BM1184", "BM1167", "BM3648", "BM1205", "BM1206", "BM1208", "BM1170", "BM3651", "BM3652", "BM1172" };
        datatypes.datasetRAW _data = new datatypes.datasetRAW();
        datatypes.pcode _code = new datatypes.pcode();
        datatypes.product _prod = new datatypes.product();
        datatypes.part _part = new datatypes.part();
        datatypes.part part_empty = new datatypes.part();
        List<datatypes.part> no_parts = new List<datatypes.part>();
        Write w = new Write();

        List<datatypes.part> partList = new List<datatypes.part>();
        string _S = "                                                                                     ";
        string _L = "_______________________________________________________________________________";
        int number = 0;
        NumberStyles style = NumberStyles.AllowExponent | NumberStyles.AllowDecimalPoint | NumberStyles.Float;
        IFormatProvider culture = CultureInfo.CreateSpecificCulture("en-US");
        public datatypes.datasetRAW parse(List<string> dataset)
        {
            no_parts.Add(part_empty);
            no_parts.Add(part_empty);
            no_parts.Add(part_empty);
            part_empty._part = "NoPart";
            _data = new datatypes.datasetRAW();
            _data.pcodes = new List<datatypes.pcode>();
            if (dataset[0].ToLower().Contains("error")) { w.ErrorWriter(dataset[0]); }
            foreach (string rawLine in dataset)
            {
                _code = new datatypes.pcode();
                if (!rawLine.Contains('╥')) { continue; }
                List<string> line = rawLine.Split('╥').ToList();
                List<string> RAWpcode = line[1].Split(',').ToList();
                bool r0 = decimal.TryParse(RAWpcode[1].Trim(), style, culture, out decimal o0);
                bool r1 = decimal.TryParse(RAWpcode[2].Trim(), style, culture, out decimal o1);
                bool r2 = decimal.TryParse(RAWpcode[3].Trim(), style, culture, out decimal o2);
                bool r3 = int.TryParse(RAWpcode[4].Trim(), out int o3);
                _code._pcode = RAWpcode[0].Trim();
                _code.totalNeeded = Math.Round(o0, 2, MidpointRounding.AwayFromZero);
                _code.days30 = Math.Round(o1, 2, MidpointRounding.AwayFromZero);
                _code.hoursAssembled = Math.Round(o2, 2, MidpointRounding.AwayFromZero);
                _code.dayLimit = o3;
                if (String.IsNullOrEmpty(line[0])) { _data.pcodes.Add(_code); continue; }
                List<string> products = line[0].Split('╘').ToList();
                products.RemoveAt(0);
                if (products.Count() <= 1)
                {
                    _data.pcodes.Add(_code);
                    continue;
                }
                else
                {
                    _code.productList = new List<datatypes.product>();
                    foreach (string p in products)
                    {
                        //decimal potential = -1;
                        _prod = new datatypes.product();
                        _prod.lowParts = new List<datatypes.part>();
                        string pdataRAW = p.Split('╒').ToList().Last();
                        List<string> prodData = pdataRAW.Split(',').ToList();
                        bool rr0 = decimal.TryParse(prodData[0].Trim(), style, culture, out decimal need);
                        if (prodData[1].Contains('E')) { number += 1; }
                        bool rr1 = decimal.TryParse(prodData[1].Trim(), style, culture, out decimal days30);
                        if (rr0) { _prod.need = Math.Round(need, 2, MidpointRounding.AwayFromZero); }
                        if (rr1) { _prod.days30 = Math.Round(days30, 6, MidpointRounding.AwayFromZero); }
                        p.Remove(p.Last());
                        List<string> partsRaw = p.Split('╒').ToList();
                        partsRaw.Remove(partsRaw.Last());
                        List<String> parts = partsRaw[0].Split('╙').ToList();
                        List<string> _prodData = parts[0].Split(',').ToList();
                        _prod._product = _prodData[0].Trim();
                        _prod.desc = _prodData[1].Trim();
                        bool _rr0 = decimal.TryParse(_prodData[2].Trim(), out decimal p0);
                        bool _rr1 = decimal.TryParse(_prodData[3].Trim(), out decimal p1);
                        bool _rr2 = decimal.TryParse(_prodData[4].Trim(), out decimal p2);
                        if (_rr0) { _prod.yu = Math.Round(p0, 2, MidpointRounding.AwayFromZero); }
                        if (_rr1) { _prod.oh = Math.Round(p1, 2, MidpointRounding.AwayFromZero); }
                        if (_rr2) { _prod.ds = Math.Round(p2, 2, MidpointRounding.AwayFromZero); }
                        partList = new List<datatypes.part>();
                        if (parts.Count() > 1)
                        {
                            parts.RemoveAt(0);
                            foreach (string part in parts)
                            {
                                _part = new datatypes.part();
                                string[] four = part.Split(',');
                                if (four.Count() == 5)
                                {
                                    _part._part = four[0];
                                    bool rrr0 = decimal.TryParse(four[1].Trim(), style, culture, out decimal oh);
                                    bool rrr1 = decimal.TryParse(four[2].Trim(), style, culture, out decimal yu);
                                    bool rrr2 = decimal.TryParse(four[3].Trim(), style, culture, out decimal qn);
                                    bool rrr3 = int.TryParse(four[4].Trim(), out int qa);
                                    if (rrr0) { _part.oh = Math.Round(oh, 2, MidpointRounding.AwayFromZero); }
                                    if (rrr1) { _part.yu = Math.Round(yu, 2, MidpointRounding.AwayFromZero); }
                                    if (rrr2) { _part.qn = Math.Round(qn, 2, MidpointRounding.AwayFromZero); }
                                    if (rrr3) { _part.qa = qa; }
                                    if (_part.oh != 0 && _part.yu != 0)
                                    {
                                        _part.ds = (_part.oh / _part.yu) * 365;
                                        _part.ds = Math.Round(_part.ds, 0, MidpointRounding.AwayFromZero);
                                    }
                                    bool pass = false;
                                    foreach(string f in filter)
                                    {
                                        if (_part._part.Contains(f))
                                        {
                                            pass = true;
                                        }
                                    }
                                    if (pass == true) { continue; }
                                    else { partList.Add(_part); }
                                }
                            }
                        }
                        if (partList.Count > 0)
                        {
                            partList = partList.OrderBy(x => x.ds).ToList();
                            _prod.lowParts.Add(partList[0]);
                            //if (partList.Count > 1) { _prod.lowParts.Add(partList[1]); }
                            //if (partList.Count > 2) { _prod.lowParts.Add(partList[2]); }
                            //Product do no exceed = ((years use / 365) * estimated day supply) - (on hand complete - quantity assembled) This equation on moronic and is probably wrong.
                            foreach(datatypes.part __part in _prod.lowParts)
                            {
                                _prod.doNotExceed = Math.Round(((__part.yu / 365) * __part.ds) - (__part.oh - __part.qa), 0, MidpointRounding.AwayFromZero); ;
                            }
                        }
                        else
                        {
                            _prod.lowParts = no_parts;
                        }
                        _code.productList.Add(_prod);
                    }
                    _code.productList = _code.productList.OrderBy(x => x.ds).ToList();
                    _data.pcodes.Add(_code);
                }
            }
            //_data.pcodes = _data.pcodes.OrderBy(x => x._pcode).ToList();
            return _data;
        }
    }
}
