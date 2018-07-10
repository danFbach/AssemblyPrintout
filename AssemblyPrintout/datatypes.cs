using System;
using System.Collections.Generic;

namespace AssemblyPrintout
{
	class datatypes
	{
		public class dailyAvgs2017
		{
			public string _1 = "125.92";
			public string _2 = "108.41";
			public string _3 = "119.51";
			public string _4 = "111.91";
			public string _5 = "120.81";
			public string _6 = "113.30";
			public string _7 = "104.81";
			public string _8 = "121.38";
			public string _9 =  "88.39";
			public string _10 = "164.54";
			public string _11 = "168.17";
			public string _12 = "184.28";

		}
		public class year
		{
			public List<month> months { get; set; }
		}
		public class month
		{
			public decimal avgDailyHours { get; set; }

		}
		public class aProductionPack
		{
			public string productNumber { get; set; }
			public decimal numberAssembled { get; set; }

		}
		public class path
		{
			public string exportData = @"C:\INVEN\EXPORT.TXT";
			public string exportData2 = @"C:\Users\Dan\Documents\Visual Studio 2017\Projects\AssemblyPrintout\AssemblyPrintout\data\EXPORT.TXT";
			public string yestPrdctn = @"\\SOURCE\INVEN\TEMPDATA\YEST.TXT";
			public string production = @"\\SOURCE\INVEN\PRODUCTS.BAK";
			public string asmblyData = @"\\SOURCE\INVEN\PDATA.TXT";
			public string today = @"\\SOURCE\INVEN\TEMPDATA\TODAY.TXT";
			public string yesterday = @"\\SOURCE\INVEN\TEMPDATA\YEST.TXT";
			public string month = @"\\SOURCE\INVEN\TEMPDATA\MONTH.TXT";
			public string qbError = @"C:\INVEN\qberror.txt";
			public string d7 = @"C:\INVEN\Daily_7.txt";
			public string assembly = @"C:\INVEN\Assembly_Schedule.txt";
			public string notepad = "Notepad.exe";
			public string totalProduction2017 = @"C:\Users\Dan\Documents\Visual Studio 2017\Projects\AssemblyPrintout\AssemblyPrintout\data\TOTPROD2017.TXT";
		}
		public class datasetRAW
		{
			public List<pcode> pcodes { get; set; }
			public decimal assembledHours { get; set; }
			public decimal XdaysSupply { get; set; }
			public Daily7Data d7d { get; set; }
			public decimal annualAssemblyHours { get; set; }
			public Decimal prodSurplusHr30 { get; set; }
			public Decimal prodSurplusHr60 { get; set; }
			public Decimal prodSurplusHr90 { get; set; }
			public decimal prodHrNeedthirty { get; set; }
			public decimal prodHrNeedsixty { get; set; }
			public decimal prodHrNeedninety { get; set; }
		}

		public class pcode
		{
			public string _pcode { get; set; }

			public decimal totalNeeded { get; set; }

			//hours to make 30 day supply
			public decimal XdaysSupply { get; set; }

			public decimal hoursAssembled { get; set; }
			public List<product> productList { get; set; }
			public int dayLimit { get; set; }
			public decimal mblyHours { get; set; }
		}
		public class product
		{
			public string _product { get; set; }
			public string desc { get; set; }

			//on hand quantity
			public decimal oh { get; set; }

			//years use
			public decimal yu { get; set; }

			//days supply
			public decimal ds { get; set; }

			//needed
			public decimal need { get; set; }

			//hours for 30-day supply
			public decimal assemblyTime { get; set; }

			//potential for assembly
			public decimal doNotExceed { get; set; }

			public List<part> lowParts { get; set; }
			public int ytdSales { get; set; }
			public decimal xDayHours { get; set; }
			public decimal annualAssembly { get; set; }
		}
		public class part
		{
			public string _part { get; set; }

			//on hand quantity
			public decimal oh { get; set; }

			//years use
			public decimal yu { get; set; }

			//quantity needed for assembly
			public decimal qn { get; set; }

			//estimated dats supply
			public decimal ds { get; set; }

			//quantity assembled
			public int qa { get; set; }
		}
		public class Daily7Data
		{
			public List<string> partNumbers { get; set; }
			public string hoursForYearsSales { get; set; }
			public string prodHoursPerDay { get; set; }
			public string totalHours { get; set; }
			public string assembledHours { get; set; }
			public string hoursNeeded30 { get; set; }
			public string surplusHours30 { get; set; }
			public string hoursNeeded60 { get; set; }
			public string surplusHours60 { get; set; }
			public string hoursNeeded90 { get; set; }
			public string surplusHours90 { get; set; }
		}
		public class hours
		{
			public decimal today { get; set; }
			public decimal yesterday { get; set; }
			public decimal monthAvg { get; set; }
		}
		public class productionDataPack
		{
			public List<productionLine> today { get; set; }
			public List<productionLine> yesterday { get; set; }
			public List<productionLine> month { get; set; }
		}
		public class productionLine
		{
			public int produced { get; set; }
			public decimal assemblyTime { get; set; }
		}
		public class assemblyTimes
		{
			public Dictionary<string, decimal> dict { get; set; }
		}
	}
}
