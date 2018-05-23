using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace AssemblyPrintout
{
    class Parser
    {
        #region globalVars
        List<string> filter = new List<string> { "LB", "MR", "BM1167" };
        datatypes.datasetRAW _data = new datatypes.datasetRAW();
        datatypes.pcode _code = new datatypes.pcode();
        datatypes.product _prod = new datatypes.product();
        datatypes.part _part = new datatypes.part();
        Write w = new Write();
        datatypes.part emptyPart = new datatypes.part();
        List<datatypes.part> partList = new List<datatypes.part>();
        utils u = new utils();
        int j30 = 0;
        NumberStyles style = NumberStyles.AllowExponent | NumberStyles.AllowDecimalPoint | NumberStyles.Float;
        IFormatProvider culture = CultureInfo.CreateSpecificCulture("en-US");

        public Parser()
        {
             j30 = u.getj30();
        }
        #endregion globalVars
        #region lineParserSwitch
        public datatypes.datasetRAW _parser(List<string> dataset)
        {
            emptyPart._part = "NoPart";
            datatypes.datasetRAW pcodes = new datatypes.datasetRAW();
            pcodes.pcodes = new List<datatypes.pcode>();
            //List<datatypes.pcode> pcodes = new List<datatypes.pcode>();
            datatypes.pcode code = new datatypes.pcode();
            code.productList = new List<datatypes.product>();
            List<datatypes.product> productList = new List<datatypes.product>();
            datatypes.product prod = new datatypes.product();
            prod.lowParts = new List<datatypes.part>();
            datatypes.part part = new datatypes.part();
            datatypes.Daily7Data daily7Data = new datatypes.Daily7Data();

            foreach (string line in dataset)
            {
                switch (line[0])
                {
                    case '╝':
                        //a code line                        
                        code = new datatypes.pcode();
                        productList = new List<datatypes.product>();
                        code = parseCodeNum(line);
                        continue;
                    case '╘':
                        //a product line
                        prod = parseProduct(line);
                        if (prod.need < 0 || prod.Equals(null)) { continue; }
                        else { productList.Add(prod); continue; }
                    case '╥':
                        //code data line
                        datatypes.pcode tempCode = new datatypes.pcode();
                        tempCode = parseCode(line);
                        code.dayLimit = tempCode.dayLimit;
                        code.hoursAssembled = tempCode.hoursAssembled;
                        code.XdaysSupply = Math.Round(productList.Sum(x => x.XdaysSupply), 2, MidpointRounding.AwayFromZero);
                        code.totalNeeded = Math.Round(productList.Sum(x => x.need), 2, MidpointRounding.AwayFromZero);
                        code.productList = productList.OrderBy(x => x.ds).ToList();
                        pcodes.pcodes.Add(code);
                        continue;
                    case '╗':
                        //beginning of daily_7 data, parts list
                        daily7Data = daily7Parser(line);
                        pcodes.daily7Data = daily7Data;
                        continue;
                    default:
                        continue;
                }
            }
            pcodes.assembledHours = Math.Round(pcodes.pcodes.Sum(x => x.hoursAssembled), 2, MidpointRounding.AwayFromZero);
            pcodes.XdaysSupply = Math.Round(pcodes.pcodes.Sum(x => x.XdaysSupply), 1, MidpointRounding.AwayFromZero);
            return pcodes;
        }
        #endregion lineParserSwitch
        #region dataParser
        public datatypes.pcode parseCode(string CodeRAW)
        {
            _code = new datatypes.pcode();
            List<string> line = CodeRAW.Split('╥').ToList();
            List<string> RAWpcode = line[1].Split(',').ToList();
            bool r2 = decimal.TryParse(RAWpcode[0].Trim(), style, culture, out decimal o2);
            bool r3 = int.TryParse(RAWpcode[1].Trim(), out int o3);
            _code.hoursAssembled = Math.Round(o2, 2, MidpointRounding.AwayFromZero);
            _code.dayLimit = o3;
            return _code;
        }
        public datatypes.pcode parseCodeNum(string codeDataRAW)
        {
            datatypes.pcode _code = new datatypes.pcode();
            _code._pcode = codeDataRAW.Split('╝').Last().Trim();
            return _code;
        }
        public datatypes.product parseProduct(string ProductRAW)
        {
            string[] productDataRAW = ProductRAW.Split('╘').Last().Split('╒');
            List<string> numbers = productDataRAW.Last().Split(',').ToList();
            List<string> product = productDataRAW.First().Split('╙').ToList();
            List<string> a_product = product[0].Split(',').ToList();
            decimal ytdSales = 0;
            _prod = new datatypes.product();
            _prod.lowParts = new List<datatypes.part>();
            _prod._product = a_product[0].Trim();
            _prod.desc = a_product[1].Trim();
            bool _rr0 = decimal.TryParse(a_product[2].Trim(), out decimal p0);
            bool _rr1 = decimal.TryParse(a_product[3].Trim(), out decimal p1);
            bool _rr2 = decimal.TryParse(a_product[4].Trim(), out decimal p2);
            bool _rr3 = decimal.TryParse(a_product[5].Trim(), out decimal p3);
            if (_rr0) { _prod.yu = Math.Round(p0, 2, MidpointRounding.AwayFromZero); }
            if (_rr1) { _prod.oh = Math.Round(p1, 2, MidpointRounding.AwayFromZero); }
            if (_rr2) { _prod.ds = Math.Round(p2, 2, MidpointRounding.AwayFromZero); }
            if (_rr3) { ytdSales = Math.Round(p3, 2, MidpointRounding.AwayFromZero); }

            decimal asdf = ((_prod.yu / 365) * _code.dayLimit) - _prod.oh;
            decimal asff = ((ytdSales / j30) * _code.dayLimit) - _prod.oh;
            if(asdf > asff) { _prod.need = asdf; } else { _prod.need = asff; }
            _prod.need = Math.Round(_prod.need, 0, MidpointRounding.AwayFromZero);
            bool rr0 = decimal.TryParse(numbers[0].Trim(), style, culture, out decimal days30);
            if (rr0) { _prod.XdaysSupply = Math.Round(days30, 2, MidpointRounding.AwayFromZero); }

            product.RemoveAt(0);
            partList = new List<datatypes.part>();
            if (product.Count() > 0)
            {
                foreach (string part in product)
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
                        foreach (string f in filter)
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
                partList = partList.OrderBy(x => x.ds).ToList();
                _prod.lowParts.Add(partList[0]);
                _prod.doNotExceed = Math.Round(((_prod.yu / 365) * _prod.lowParts[0].ds) - _prod.oh, 0, MidpointRounding.AwayFromZero);
            }
            else
            {
                _prod.lowParts.Add(emptyPart);
            }
            return _prod;
        }
        #endregion dataParser
        #region daily7parse
        public datatypes.Daily7Data daily7Parser(string line)
        {
            datatypes.Daily7Data daily7Data = new datatypes.Daily7Data();
            daily7Data.partNumbers = new List<string>();
            string[] _line = line.Split('╗');
            string[] data = _line[1].Split('└');
            List<string> parts = data[0].Split(',').ToList();
            foreach (string part in parts) { daily7Data.partNumbers.Add(part.Trim()); }
            string[] _data = data[1].Split(',');
            daily7Data.hoursForYearsSales = _data[0];
            daily7Data.prodHoursPerDay = _data[1];
            daily7Data.totalHours = _data[2];
            daily7Data.assembledHours = _data[3];
            daily7Data.hoursNeeded30 = _data[4];
            daily7Data.surplusHours30 = _data[5];
            daily7Data.hoursNeeded60 = _data[6];
            daily7Data.surplusHours60 = _data[7];
            daily7Data.hoursNeeded90 = _data[8];
            daily7Data.surplusHours90 = _data[9];
            return daily7Data;
        }
        #endregion daily7parse
        #region deprecatedCode
        public datatypes.datasetRAW parse(List<string> dataset)
        {
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
                _code.hoursAssembled = Math.Round(o2, 2, MidpointRounding.AwayFromZero);
                _code.dayLimit = o3;
                if (String.IsNullOrEmpty(line[0])) { _data.pcodes.Add(_code); continue; }
                List<string> products = line[0].Split('╘').ToList();
                products.RemoveAt(0);
                if (products.Count() <= 1) { _data.pcodes.Add(_code); continue; }
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
                        bool rr1 = decimal.TryParse(prodData[1].Trim(), style, culture, out decimal days30);
                        if (rr0) { _prod.need = Math.Round(need, 0, MidpointRounding.AwayFromZero); }
                        if (rr1) { _prod.XdaysSupply = Math.Round(days30, 6, MidpointRounding.AwayFromZero); }
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
                                    foreach (string f in filter)
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
                            foreach (datatypes.part __part in _prod.lowParts)
                            {
                                _prod.doNotExceed = Math.Round(((_prod.yu / 365) * __part.ds) - _prod.oh, 0, MidpointRounding.AwayFromZero);
                            }
                        }

                        if (_prod.need < 0) { continue; }
                        else { _code.productList.Add(_prod); }
                    }
                    foreach (datatypes.product p in _code.productList)
                    {
                        _code.totalNeeded += p.need;
                        _code.XdaysSupply += p.XdaysSupply;
                    }
                    _code.totalNeeded = Math.Round(_code.totalNeeded, 0, MidpointRounding.AwayFromZero);
                    _code.XdaysSupply = Math.Round(_code.XdaysSupply, 2, MidpointRounding.AwayFromZero);
                    _code.productList = _code.productList.OrderBy(x => x.ds).ToList();
                    _data.pcodes.Add(_code);
                }
            }
            //_data.pcodes = _data.pcodes.OrderBy(x => x._pcode).ToList();
            return _data;
        }
        #endregion deprecatedCode
    }

}
