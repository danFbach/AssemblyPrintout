using System;
using System.Collections.Generic;

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
			List<string> d = r.reader(@"C:\INVEN\EXPORT.txt");
			datatypes.datasetRAW dsr = p._parser(d);
			w.customWriter(dsr, u.getPath("assembly"), u.getPath("daily7"));
		}
    }
}
