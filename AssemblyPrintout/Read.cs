using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace AssemblyPrintout
{
    class Read
    {
        public List<string> reader()
        {
            string path = "C:/INVEN/NEWEXPORT.txt";
            List<string> data = new List<string>();
            if (File.Exists(path))
            {
                try
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
                catch (Exception e)
                {
                    Console.WriteLine("File is unreadable");
                    Console.WriteLine(e.Message);
                }
                return data;
            }
            else
            {
                Console.Write("Source file does not exist");
                return null;
            }
        }
    }
}
