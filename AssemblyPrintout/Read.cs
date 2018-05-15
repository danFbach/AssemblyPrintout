using System;
using System.Collections.Generic;
using System.IO;

namespace AssemblyPrintout
{
    class Read
    {
        public List<string> reader()
        {
            string path = "C:\\INVEN\\EXPORT.txt";
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
    }
}
