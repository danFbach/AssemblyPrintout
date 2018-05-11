using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using static AssemblyPrintout.datatypes;

namespace AssemblyPrintout
{
    class Parser
    {
        datasetRAW _data = new datasetRAW();
        pcode _code = new pcode();
        product _prod = new product();
        part _part = new part();
        part part_empty = new part();
        Write w = new Write();

        List<part> partList = new List<part>();
        string _S = "                                                                                     ";
        string _L = "_______________________________________________________________________________";
        int number = 0;
        NumberStyles style = NumberStyles.AllowExponent | NumberStyles.AllowDecimalPoint | NumberStyles.Float;
        IFormatProvider culture = CultureInfo.CreateSpecificCulture("en-US");
        public datasetRAW parse(List<string> dataset)
        {
            part_empty._part = "No Part";
            _data = new datasetRAW();
            _data.pcodes = new List<pcode>();
            if (dataset[0].ToLower().Contains("error")) { w.ErrorWriter(dataset[0]); }
            foreach (string rawLine in dataset)
            {
                _code = new pcode();
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
                    _code.productList = new List<product>();
                    foreach (string p in products)
                    {
                        decimal potential = -1;
                        _prod = new product();
                        _prod.lowPart = new part();
                        string pdataRAW = p.Split('╒').ToList().Last();
                        List<string> prodData = pdataRAW.Split(',').ToList();
                        bool rr0 = decimal.TryParse(prodData[0].Trim(), style, culture, out decimal need);
                        if (prodData[1].Contains('E')) { number += 1; }
                        bool rr1 = decimal.TryParse(prodData[1].Trim(), style, culture, out decimal days30);
                        if (rr0) { _prod.need = Math.Round(need, 2, MidpointRounding.AwayFromZero); }
                        if (rr1) { _prod.days30 = days30; }
                        p.Remove(p.Last());
                        List<String> parts = p.Split('╙').ToList();
                        List<string> _prodData = parts[0].Split(',').ToList();
                        _prod._product = _prodData[0].Trim();
                        _prod.desc = _prodData[1].Trim();
                        bool _rr0 = decimal.TryParse(_prodData[2].Trim(), out decimal p0);
                        bool _rr1 = decimal.TryParse(_prodData[3].Trim(), out decimal p1);
                        bool _rr2 = decimal.TryParse(_prodData[4].Trim(), out decimal p2);
                        if (_rr0) { _prod.yu = Math.Round(p0, 2, MidpointRounding.AwayFromZero); }
                        if (_rr1) { _prod.oh = Math.Round(p1, 2, MidpointRounding.AwayFromZero); }
                        if (_rr2) { _prod.ds = Math.Round(p2, 2, MidpointRounding.AwayFromZero); }
                        partList = new List<part>();
                        if (parts.Count() > 1)
                        {
                            parts.RemoveAt(0);
                            foreach (string part in parts)
                            {
                                _part = new part();
                                string[] four = part.Split(',');
                                if (four.Count() == 5)
                                {
                                    _part._part = four[0];
                                    bool rrr0 = decimal.TryParse(four[1].Trim(), style, culture, out decimal oh);
                                    bool rrr1 = decimal.TryParse(four[2].Trim(), style, culture, out decimal yu);
                                    bool rrr2 = decimal.TryParse(four[3].Trim(), style, culture, out decimal qn);
                                    bool rrr3 = int.TryParse(four[4].Trim(), out int qa);
                                    if (rrr0) { _part.oh = oh; }
                                    if (rrr1) { _part.yu = yu; }
                                    if (rrr2) { _part.qn = qn; }
                                    if (rrr3) { _part.qa = qa; }
                                    if (_part.oh != 0 && _part.yu != 0)
                                    {
                                        _part.ds = (_part.oh / _part.yu) * 365;
                                    }
                                    partList.Add(_part);
                                }
                            }
                        }
                        if (partList.Count > 0)
                        {
                            partList = partList.OrderBy(x => x.ds).ToList();
                            _prod.lowPart = partList[0];
                            //Product do no exceed = ((years use / 365) * estimated day supply) - (on hand complete - quantity assembled) This equation on moronic and is probably wrong.
                            _prod.doNotExceed = Math.Round(((_prod.lowPart.yu / 365) * _prod.lowPart.ds) - (_prod.lowPart.oh - _prod.lowPart.qa), 2, MidpointRounding.AwayFromZero); ;
                        }
                        else
                        {
                            _prod.lowPart = part_empty;
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
