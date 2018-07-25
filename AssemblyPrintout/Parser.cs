using System;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using static AssemblyPrintout.datatypes;

namespace AssemblyPrintout
{
	class Parser
	{
		#region globalVarsAndConst
		string _ = Environment.NewLine;
		List<string> filter = new List<string> { "LB", "MR", "SL1698" };
		datasetRAW _data = new datasetRAW( );
		pcode _code = new pcode( );
		product _prod = new product( );
		part _part = new part( );
		part emptyPart = new part( );
		List<part> partList = new List<part>( );
		Write w = new Write( );
		utils u = new utils( );
		Read r = new Read( );
		paths path = new paths( );
		int j30 = 0;
		NumberStyles style = NumberStyles.AllowExponent | NumberStyles.AllowDecimalPoint | NumberStyles.Float;
		IFormatProvider culture = CultureInfo.CreateSpecificCulture("en-US");
		public Parser()
		{
			j30 = u.getj30( );
		}
		#endregion globalVars
		#region lineParserSwitch
		public datasetRAW _parser(List<string> dataset)
		{
			emptyPart._partName = "NoPart";
			datasetRAW pcodes = new datasetRAW( );
			pcodes.pcodes = new List<pcode>( );
			pcode code = new pcode( );
			code.productList = new List<product>( );
			List<product> productList = new List<product>( );
			List<string> productRawPack = new List<string>( );
			product prod = new product( );
			prod.lowParts = new List<part>( );
			part part = new part( );
			Daily7Data daily7Data = new Daily7Data( );
			List<string> annual = dataset.Last( ).Split('⌡').ToList( );
			dataset.RemoveAt(dataset.IndexOf(dataset.Last( )));
			neededAndAnnual naa = getAnnualUseHours(annual);
			try
			{
				foreach(string line in dataset)
				{
					switch(line[0])
					{
						case '╝':
							//a code line                        
							code = new pcode( );
							productList = new List<product>( );
							productRawPack = new List<string>( );
							code = parsePCodeNum(line);
							continue;
						case '╘':
							//a product line
							productRawPack.Add(line);
							continue;
						case '╥':
							//code data line
							pcode tempCode = new pcode( );
							tempCode = parsePCode(line);
							foreach(string productLine in productRawPack)
							{
								product tempProduct = parseProductsAndParts(productLine, tempCode.dayLimit);
								if(tempProduct.need > 0)
								{
									productList.Add(tempProduct);
								}
								else { continue; }
							}
							code.dayLimit = tempCode.dayLimit;
							code.hoursAssembled = tempCode.hoursAssembled;
							code.XdaysSupply = Math.Round(productList.Sum(x => x.xDayHours), 1, MidpointRounding.AwayFromZero);
							code.totalNeeded = Math.Round(productList.Sum(x => x.need), 0, MidpointRounding.AwayFromZero);
							code.productList = productList.OrderBy(x => x.ds).ToList( );
							pcodes.pcodes.Add(code);
							continue;
						case '╗':
							//beginning of daily_7 data, parts list
							daily7Data = daily7Parser(line);
							pcodes.d7d = daily7Data;
							List<string> hrs = new List<string>(r.genericRead(path.yestPrdctn));
							if(hrs.Count > 0) { pcodes.YesterdaysProductionHours = hrs[0]; } else { pcodes.YesterdaysProductionHours = "0.00"; }
							continue;
						default:
							continue;
					}
				}
			}
			catch(Exception e)
			{
				w.ErrorWriter(DateTime.Now.ToShortDateString( ) + " - " + DateTime.Now.ToShortTimeString( ) + _ + "Stopped at parser switch." + _ + e.InnerException + _ + " " + e.Message + _ + e.StackTrace); Environment.Exit(0);
			}

			try
			{
				pcodes.annualAssemblyHours = naa.annualHours;
				pcodes.assembledHours = Math.Round(pcodes.pcodes.Sum(x => x.hoursAssembled), 2, MidpointRounding.AwayFromZero);
				pcodes.XdaysSupply = Math.Round(pcodes.pcodes.Sum(x => x.XdaysSupply), 1, MidpointRounding.AwayFromZero);
				if(pcodes.pcodes.Count > 0)
				{
					pcodes.prodSurplusHr30 = Math.Round((pcodes.assembledHours - ((pcodes.annualAssemblyHours / 250) * 30)), 2, MidpointRounding.AwayFromZero);
					pcodes.prodSurplusHr60 = Math.Round((pcodes.assembledHours - ((pcodes.annualAssemblyHours / 250) * 60)), 2, MidpointRounding.AwayFromZero);
					pcodes.prodSurplusHr90 = Math.Round((pcodes.assembledHours - ((pcodes.annualAssemblyHours / 250) * 90)), 2, MidpointRounding.AwayFromZero);
					pcodes.prodHrNeedthirty = naa.needed30;
					pcodes.prodHrNeedsixty = naa.needed60;
				}
				else
				{
					pcodes.prodSurplusHr30 = 0;
					pcodes.prodSurplusHr60 = 0;
					pcodes.prodSurplusHr90 = 0;
					pcodes.prodHrNeedthirty = 0;
					pcodes.prodHrNeedsixty = 0;
					pcodes.prodHrNeedninety = 0;
				}
			}
			catch(Exception e)
			{
				w.ErrorWriter(DateTime.Now.ToShortDateString( ) + " - " + DateTime.Now.ToShortTimeString( ) + _ + "Stopped after parser switch." + _ + e.InnerException + _ + " " + e.Message + _ + e.StackTrace); Environment.Exit(0);
			}
			return pcodes;
		}
		#endregion lineParserSwitch
		#region assembly utils
		public neededAndAnnual getAnnualUseHours(List<string> raw)
		{
			neededAndAnnual naa = new neededAndAnnual( );
			naa.annualHours = 0;
			naa.needed30 = 0;
			naa.needed60 = 0;
			foreach(string r in raw)
			{

				if(string.IsNullOrEmpty(r)) { continue; }
				string[] _r = r.Split(',');
				bool r1 = decimal.TryParse(_r[0], out decimal annualUse);
				bool r0 = decimal.TryParse(_r[1], out decimal assemblyTime);
				bool r2 = decimal.TryParse(_r[2], out decimal onHand);
				if(r1 && r0) { naa.annualHours += ((annualUse * assemblyTime) / 3600); }
				decimal daily = (annualUse / 365);
				decimal d30 = ((annualUse / 365) * 30);
				decimal d60 = ((annualUse / 365) * 60);
				if(onHand < d30) {
					naa.needed30 += (((d30 - onHand) * assemblyTime) / 3600);
				}
				if(onHand < d60) { naa.needed60 += (((d60 - onHand) * assemblyTime) / 3600); }
			}
			naa.annualHours = Math.Round(naa.annualHours, 2, MidpointRounding.AwayFromZero);
			naa.needed30 = Math.Round(naa.needed30, 2, MidpointRounding.AwayFromZero);
			naa.needed60 = Math.Round(naa.needed60, 2, MidpointRounding.AwayFromZero);
			return naa;
		}
		//public string yesterdayProdHours(string todayhrs, List<string> raw)
		//{
		//	Read r = new Read( );
		//	Write w = new Write( );
		//	int yr = DateTime.Now.Year;
		//	int day = DateTime.Now.Day;
		//	int month = DateTime.Now.Month;
		//	//string dt = month.ToString() + "-" + day.ToString() + "-" + yr.ToString();
		//	string yh = r.reader(@"\\SOURCE\INVEN\YESTHRS.TXT").First( );
		//	string tmrw = r.reader(@"\\SOURCE\INVEN\TMRWHRS.TXT").First( );
		//	string[] data = yh.Split('|');

		//	string lastWorkDay = "";
		//	if(DateTime.Now.DayOfWeek == DayOfWeek.Monday) { lastWorkDay = DateTime.Now.AddDays(-3).ToShortDateString( ); }
		//	else { lastWorkDay = DateTime.Now.AddDays(-1).ToShortDateString( ); }
		//	//if the data for today has already been made, just return the created data.
		//	if(data[0] == lastWorkDay)
		//	{   //return stored yesterdays hours
		//		return data[1];
		//	}
		//	else
		//	{
		//		decimal auh = getAnnualUseHours(raw);
		//		w.genericLineWriter(tmrw, @"\\SOURCE\INVEN\YESTHRS.TXT");
		//		w.genericLineWriter(DateTime.Now.ToShortDateString( ) + "|" + auh.ToString( ), @"\\SOURCE\INVEN\TMRWHRS.TXT");
		//	}
		//	return "";
		//}
		#endregion assembly utils
		#region dataParser
		public datatypes.pcode parsePCode(string CodeRAW)
		{
			_code = new datatypes.pcode( );
			List<string> line = CodeRAW.Split('╥').ToList( );
			List<string> RAWpcode = line[1].Split(',').ToList( );
			bool r2 = decimal.TryParse(RAWpcode[0].Trim( ), style, culture, out decimal o2);
			bool r3 = int.TryParse(RAWpcode[1].Trim( ), out int o3);
			_code.hoursAssembled = Math.Round(o2, 2, MidpointRounding.AwayFromZero);
			_code.dayLimit = o3;
			return _code;
		}
		public datatypes.pcode parsePCodeNum(string codeDataRAW)
		{
			datatypes.pcode _code = new datatypes.pcode( );
			_code._pcode = codeDataRAW.Split('╝').Last( ).Trim( );
			return _code;
		}
		public product parseProductsAndParts(string ProductRAW, int dayLimit)
		{
			string[] productDataRAW = ProductRAW.Split('╘').Last( ).Split('╒');
			List<string> numbers = productDataRAW.Last( ).Split(',').ToList( );
			List<string> product = productDataRAW.First( ).Split('╙').ToList( );
			List<string> a_product = product[0].Split(',').ToList( );
			_prod = new product( );
			_prod.lowParts = new List<part>( );
			_prod._product = a_product[0].Trim( );
			_prod.desc = a_product[1].Trim( );
			bool _rr0 = decimal.TryParse(a_product[2].Trim( ), out decimal p0);
			bool _rr1 = decimal.TryParse(a_product[3].Trim( ), out decimal p1);
			bool _rr2 = decimal.TryParse(a_product[4].Trim( ), out decimal p2);
			bool _rr3 = int.TryParse(a_product[5].Trim( ), out int p3);
			if(_rr0) { _prod.yu = Math.Round(p0, 2, MidpointRounding.AwayFromZero); }
			if(_rr1) { _prod.oh = Math.Round(p1, 2, MidpointRounding.AwayFromZero); }
			if(_rr2) { _prod.ds = Math.Round(p2, 2, MidpointRounding.AwayFromZero); }
			if(_rr3) { _prod.ytdSales = p3; }
			if(j30 > 60)
			{
				decimal yuCalc = ((_prod.yu / 365) * dayLimit) - _prod.oh;
				decimal ytdCalc = ((_prod.ytdSales / j30) * dayLimit) - _prod.oh;
				if(yuCalc > ytdCalc) { _prod.need = yuCalc; } else { _prod.need = ytdCalc; }
			}
			else
			{
				_prod.need = (((_prod.yu / 365) * dayLimit) - _prod.oh);
			}
			_prod.need = Math.Round(_prod.need, 0, MidpointRounding.AwayFromZero);

			if(_prod.need > 0)
			{
				bool rr1 = decimal.TryParse(numbers[1].Trim( ), style, culture, out decimal assemblyTime);
				if(rr1) { _prod.assemblyTime = assemblyTime; }
				_prod.xDayHours = ((_prod.need * _prod.assemblyTime) / 3600);
				if(_prod.xDayHours < 1) { _prod.xDayHours = Math.Round(_prod.xDayHours, 3, MidpointRounding.AwayFromZero); }
				else { _prod.xDayHours = Math.Round(_prod.xDayHours, 2, MidpointRounding.AwayFromZero); }
				product.RemoveAt(0);
				partList = new List<datatypes.part>( );
				if(product.Count( ) > 0)
				{
					foreach(string part in product)
					{
						_part = new datatypes.part( );
						string[] four = part.Split(',');
						if(four.Count( ) == 5)
						{
							_part._partName = four[0];
							bool rrr0 = decimal.TryParse(four[1].Trim( ), style, culture, out decimal oh);
							bool rrr1 = decimal.TryParse(four[2].Trim( ), style, culture, out decimal yu);
							bool rrr2 = decimal.TryParse(four[3].Trim( ), style, culture, out decimal qn);
							bool rrr3 = int.TryParse(four[4].Trim( ), out int qa);
							if(rrr0) { _part.oh = Math.Round(oh, 2, MidpointRounding.AwayFromZero); }
							if(rrr1) { _part.yu = Math.Round(yu, 2, MidpointRounding.AwayFromZero); }
							if(rrr2) { _part.qn = Math.Round(qn, 2, MidpointRounding.AwayFromZero); }
							if(rrr3) { _part.qa = qa; }
							if(_part.oh != 0 && _part.yu != 0)
							{
								_part.ds = (_part.oh / _part.yu) * 365;
								_part.ds = Math.Round(_part.ds, 0, MidpointRounding.AwayFromZero);
							}
							bool addPart = true;
							foreach(string f in filter)
							{
								if(_part._partName.ToLower( ).Contains(f.ToLower()) || _part._partName.ToLower( ).Equals(f.ToLower( )))
								{
									addPart = false;
								}
							}
							if(addPart)
							{
								partList.Add(_part); 
							}
						}
					}
					partList = partList.OrderBy(x => x.ds).ToList( );
					_prod.lowParts.Add(partList[0]);
					_prod.doNotExceed = Math.Round(((_prod.yu / 365) * _prod.lowParts[0].ds) - _prod.oh, 0, MidpointRounding.AwayFromZero);
				}
				else { _prod.lowParts.Add(emptyPart); }
			}
			return _prod;
		}
		#endregion dataParser
		#region daily7parse
		public Daily7Data daily7Parser(string line)
		{
			Daily7Data daily7Data = new Daily7Data( );
			daily7Data.partNumbers = new List<string>( );
			string[] _line = line.Split('╗');
			string[] data = _line[1].Split('└');
			List<string> parts = data[0].Split(',').ToList( );
			foreach(string part in parts) { daily7Data.partNumbers.Add(part.Trim( )); }
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
		#region globals for production hours

		#endregion globals for production hours
		#region production hours parser
		public productionDataPack GetPrdctnData(List<string> data, assemblyTimes assemblyTimes)
		{
			needToBeUpdated update = new needToBeUpdated( );
			if(File.GetLastWriteTime(path.yesterday) >= DateTime.Now.Subtract(TimeSpan.FromHours(DateTime.Now.Hour))) { update.yesterday = false; }
			else { update.yesterday = true; }
			string today = DateTime.Now.ToShortDateString( );
			string yesterday;
			if(DateTime.Today.DayOfWeek == DayOfWeek.Monday) { yesterday = DateTime.Today.Subtract(TimeSpan.FromDays(3)).ToShortDateString( ); }
			else { yesterday = DateTime.Today.Subtract(TimeSpan.FromDays(1)).ToShortDateString( ); }
			productionDataPack pdp = new productionDataPack( );
			productionLine pl;
			List<productionLine> productionDataToday = new List<productionLine>( );
			List<productionLine> productionDataYesterday = new List<productionLine>( );
			foreach(string d in data)
			{
				pl = new productionLine( );
				if(DateTime.TryParse(d.Substring(74, 10), out DateTime pDate))
				{
					if(pDate.ToShortDateString( ) == today)
					{
						if(int.TryParse(d.Substring(49, 6), out int produced))
						{
							pl.produced = produced;
							if(assemblyTimes.dict.TryGetValue(d.Substring(0, 5), out decimal assemblyTime))
							{
								pl.assemblyTime = assemblyTime;
								productionDataToday.Add(pl);
							}
						}
					}
					else if(update.yesterday && pDate.ToShortDateString( ) == yesterday)
					{
						if(int.TryParse(d.Substring(49, 6), out int produced))
						{
							pl.produced = produced;
							if(assemblyTimes.dict.TryGetValue(d.Substring(0, 5), out decimal assemblyTime))
							{
								pl.assemblyTime = assemblyTime;
								productionDataYesterday.Add(pl);
							}
						}
					}
				}
			}
			pdp.today = productionDataToday;
			//pdp.yesterday = productionDataYesterday;
			return pdp;
		}
		public hours calculateProductionTime(productionDataPack productionData)
		{
			hours _hours = new hours( );
			int numberOfDays = DateTime.Today.Day - 1;
			foreach(productionLine pl in productionData.today) { _hours.today += (pl.assemblyTime * pl.produced); }
			_hours.today = Math.Round((_hours.today / 3600), 2, MidpointRounding.AwayFromZero);
			return _hours;

			//if(productionData.yesterday.Count > 0)
			//{
			//	foreach(productionLine pl in productionData.yesterday) { _hours.yesterday += (pl.assemblyTime * pl.produced); }
			//	_hours.yesterday = Math.Round((_hours.yesterday / 3600), 2, MidpointRounding.AwayFromZero);
			//}
			//else { _hours.yesterday = 0; }
		}
		#endregion product hours parser
		#region depracated code
		public decimal calculateProductionAvg(List<productionLine> productionData)
		{
			decimal totalTime = 0;
			int numberOfDays = DateTime.Today.Day - 1;
			foreach(productionLine pl in productionData)
			{
				totalTime += (pl.assemblyTime * pl.produced);
			}
			totalTime = Math.Round(((totalTime / 3600) / numberOfDays), 2, MidpointRounding.AwayFromZero);
			return totalTime;
		}
		public List<string> getYear(List<string> d)
		{

			return d;
		}
		public year parseYear(List<string> data, assemblyTimes assemblyTimes)
		{
			string abcs = "abcdefghijklmnopqrstuvwxyz";
			year year = new year( );
			year.months = new List<month>( );
			month _1 = new month( );
			month _2 = new month( );
			month _3 = new month( );
			month _4 = new month( );
			month _5 = new month( );
			month _6 = new month( );
			month _7 = new month( );
			month _8 = new month( );
			month _9 = new month( );
			month _10 = new month( );
			month _11 = new month( );
			month _12 = new month( );
			foreach(string d in data)
			{
				if(DateTime.TryParse(d.Substring(70, 10), out DateTime pDate))
				{
					if(assemblyTimes.dict.TryGetValue(d.Substring(0, 6).Trim( ), out decimal value))
					{
						string newstring = d.Replace("ON HAND", "|");
						newstring = newstring.Replace("MADE", "|");
						string[] arr = newstring.Split('|');
						string madeRAW = "";
						foreach(char xxxxx in arr[2].ToLower( ))
						{
							if(!abcs.Contains(xxxxx))
							{
								madeRAW += xxxxx;
							}
							else { break; }
						}
						if(int.TryParse(madeRAW, out int produced))
						{
							switch(pDate.Month)
							{
								case 1:
									_1.avgDailyHours += ((produced * value) / 3600);
									continue;
								case 2:
									_2.avgDailyHours += ((produced * value) / 3600);
									continue;
								case 3:
									_3.avgDailyHours += ((produced * value) / 3600);
									continue;
								case 4:
									_4.avgDailyHours += ((produced * value) / 3600);
									continue;
								case 5:
									_5.avgDailyHours += ((produced * value) / 3600);
									continue;
								case 6:
									_6.avgDailyHours += ((produced * value) / 3600);
									continue;
								case 7:
									_7.avgDailyHours += ((produced * value) / 3600);
									continue;
								case 8:
									_8.avgDailyHours += ((produced * value) / 3600);
									continue;
								case 9:
									_9.avgDailyHours += ((produced * value) / 3600);
									continue;
								case 10:
									_10.avgDailyHours += ((produced * value) / 3600);
									continue;
								case 11:
									_11.avgDailyHours += ((produced * value) / 3600);
									continue;
								case 12:
									_12.avgDailyHours += ((produced * value) / 3600);
									continue;
							}
						}
					}
				}
			}
			_1.avgDailyHours = Math.Round((_1.avgDailyHours / 31), 2, MidpointRounding.AwayFromZero);
			year.months.Add(_1);
			_2.avgDailyHours = Math.Round((_2.avgDailyHours / 28), 2, MidpointRounding.AwayFromZero);
			year.months.Add(_2);
			_3.avgDailyHours = Math.Round((_3.avgDailyHours / 31), 2, MidpointRounding.AwayFromZero);
			year.months.Add(_3);
			_4.avgDailyHours = Math.Round((_4.avgDailyHours / 30), 2, MidpointRounding.AwayFromZero);
			year.months.Add(_4);
			_5.avgDailyHours = Math.Round((_5.avgDailyHours / 31), 2, MidpointRounding.AwayFromZero);
			year.months.Add(_5);
			_6.avgDailyHours = Math.Round((_6.avgDailyHours / 30), 2, MidpointRounding.AwayFromZero);
			year.months.Add(_6);
			_7.avgDailyHours = Math.Round((_7.avgDailyHours / 31), 2, MidpointRounding.AwayFromZero);
			year.months.Add(_7);
			_8.avgDailyHours = Math.Round((_8.avgDailyHours / 31), 2, MidpointRounding.AwayFromZero);
			year.months.Add(_8);
			_9.avgDailyHours = Math.Round((_9.avgDailyHours / 30), 2, MidpointRounding.AwayFromZero);
			year.months.Add(_9);
			_10.avgDailyHours = Math.Round((_10.avgDailyHours / 31), 2, MidpointRounding.AwayFromZero);
			year.months.Add(_10);
			_11.avgDailyHours = Math.Round((_11.avgDailyHours / 30), 2, MidpointRounding.AwayFromZero);
			year.months.Add(_11);
			_12.avgDailyHours = Math.Round((_12.avgDailyHours / 31), 2, MidpointRounding.AwayFromZero);
			year.months.Add(_12);
			return year;
		}
		#endregion depracated code
		#region POparser
		public void parseRawPO(List<string> data)
		{
			primaryPOFields PO = new primaryPOFields( );
			PO.orderRecipient = new orderRecipient( );
			PO.parts = new List<partToOrder>( );
			partToOrder part = new partToOrder( );
			PO.shipTO = new shipTO( );
			PO.POnumber = data[0];
			string shippingInfo = data[1];
			data.RemoveRange(0,2);
			foreach(string d in data)
			{
				part = new partToOrder( );
				string[] theStuff = d.Split(',');
				part.partID = theStuff[0];
				part.partDescription = theStuff[1];
				part.part_specifications = theStuff[2];
				part.specialInstruction = theStuff[3];
				if(DateTime.TryParse(theStuff[4], out DateTime deliveryDate)) { part.deliveryDate = deliveryDate; }
				else
				{
					string[] customParse = theStuff[4].Split('-');
					string toParse = customParse[0] + "/" + customParse[1] + "/" + customParse[2];
					part.deliveryDate = DateTime.Parse(toParse);
				}
				if(decimal.TryParse(theStuff[5], out decimal perPrice)) { part.perPrice = perPrice; }
				if(int.TryParse(theStuff[6], out int quan)) { part.quantity = quan; }

			}
		}
		#endregion POparser
	}

}
