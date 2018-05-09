using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AssemblyPrintout
{
    class Program
    {
        static void Main(string[] args)
        {
            Write w = new Write();
            Read r = new Read();
            Parser p = new Parser();
            utils u = new utils();

            List<string> d = r.reader();
        }
    }
}
