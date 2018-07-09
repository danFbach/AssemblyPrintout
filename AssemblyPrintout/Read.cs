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
						if (!String.IsNullOrEmpty(line.Trim()))
						{
							data.Add(line);
						}
                    }
                    sr.Close();
                }
            }
            return data;
        }
        public List<string> genericRead(string fileLoc)
        {
			List<string> data = new List<string>();
            try
			{
				if (File.Exists(fileLoc))
				{
					using (StreamReader sr = new StreamReader(fileLoc))
					{
						string line;
						while ((line = sr.ReadLine()) != null)
						{
							if (!String.IsNullOrEmpty(line.Trim()))
							{
								data.Add(line);
							}
						}
					}
					return data;
				}
				else
				{
					using (StreamWriter sw = new StreamWriter(@"C:\inven\cSharpError.txt"))
					{
						sw.WriteLine("File does not exist.");
					}
					return null;
				}
            }
            catch (Exception e)
			{
				using (StreamWriter sw = new StreamWriter(@"C:\inven\cSharpError.txt"))
				{
					sw.WriteLine("Read Error." + Environment.NewLine + e.Message + Environment.NewLine + e.InnerException + Environment.NewLine + e.StackTrace);
				}
				return null;
			}
        }
    }
}
