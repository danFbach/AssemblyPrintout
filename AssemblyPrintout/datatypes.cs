using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AssemblyPrintout
{
    class datatypes
    {
        public class datasetRAW
        {
            public List<pcode> pcodes { get; set; }
        }

        public class pcode
        {
            public string _pcode { get; set; }
            public decimal totalNeeded { get; set; }

            //hours to make 30 day supply
            public decimal days30 { get; set; }

            public decimal hoursAssembled { get; set; }
            public List<product> productList { get; set; }
            public int dayLimit { get; set; }
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
            public decimal days30 { get; set; }

            //potential for assembly
            public decimal doNotExceed { get; set; }

            public List<part> lowParts { get; set; }

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
    }
}
