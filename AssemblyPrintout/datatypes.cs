using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AssemblyPrintout
{
    class datatypes
    {
        public class pcode
        {
            int _pcode { get; set; }
            decimal totalNeeded { get; set; }

            //hours to make 30 day supply
            decimal days30 { get; set; }

            decimal hoursAssembled { get; set; }
            List<product> productList { get; set; }
        }
        public class product
        {
            string _product { get; set; }
            string desc { get; set; }

            //on hand quantity
            int oh { get; set; }

            //years use
            int yu { get; set; }

            //days supply
            int ds { get; set; }
            decimal needed { get; set; }
            decimal hours { get; set; }
            List<part> partList { get; set; }
        }
        public class part
        {
            string _part { get; set; }

            //on hand quantity
            int oh { get; set; }

            //years use
            int yu { get; set; }

            //quantity needed for assembly
            int qn { get; set; }
        }
    }
}
