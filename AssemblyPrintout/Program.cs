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
            //Export e = new Export();
            string path = u.getFilename();
            List<string> d = r.reader();
            datatypes.datasetRAW dsr = p.parse(d);
            w.Writer(dsr, path);
            //e.toPDF(path);
            //u.openPDF(path);
        }
    }
}
