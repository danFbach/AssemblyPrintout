using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AssemblyPrintout
{
    class utils
    {
        public int getj30()
        {
            DateTime now = new DateTime();
            now = DateTime.Now;            
            int this_year = DateTime.Now.Year;
            int daysSinceJ30 = 0;
            if(DateTime.Now.Month <= 6)
            {
                if(DateTime.Now.Day < 30)
                {
                    this_year -= 1;
                }
            }
            string j30 = this_year.ToString() + "-06-30T00:00:01-6:00";
            bool result = DateTime.TryParse(j30, out DateTime oldJ30);
            if (result)
            {
                TimeSpan ts = now.Subtract(oldJ30);
                daysSinceJ30 = ts.Days;
            }
            return daysSinceJ30;
        }
    }
}
