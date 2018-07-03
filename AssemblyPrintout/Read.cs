using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace AssemblyPrintout
{
    class Read
    {
        public List<string> reader(string path)
        {
            List<string> data = new List<string>();
            if (File.Exists(path))
            {
                using (StreamReader sr = new StreamReader(path))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (!String.IsNullOrEmpty(line))
                        {
                            data.Add(line);
                        }
                    }
                    sr.Close();
                }
            }
            return data;
        }
        public void genericRead(string fileLoc)
        {

            try
            {
                using (StreamReader sr = new StreamReader(fileLoc)) { }
            }
            catch (Exception e) { }
        }
    }
}
