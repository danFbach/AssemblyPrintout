using System;
using System.IO;
using System.Collections.Generic;

namespace AssemblyPrintout
{
    static class Read
    {
        static string _ = Environment.NewLine;
        public static List<string> ExportReader(string FilePath, bool isEncoded = false)
        {
            List<string> data = new List<string>();
            bool isFirst = true;
            bool dosFile = false;
            if (File.Exists(FilePath))
            {
                using (StreamReader sr = isEncoded ? new StreamReader(FilePath, System.Text.Encoding.GetEncoding(437)) : new StreamReader(FilePath, System.Text.Encoding.UTF8))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (isFirst && line.Length > 0 && line.ToCharArray()[0] != '╝')
                        {
                            if (isEncoded)
                            {
                                throw new Exception("File is of incorrect format for this program. SOWWY!");
                            }
                            else
                            {
                                dosFile = true;
                                goto outtie;
                            }

                        }
                        if (!String.IsNullOrEmpty(line.Trim()))
                        {
                            data.Add(line);
                        }
                        isFirst = false;
                    }
                    sr.Close();
                }
            outtie:
                if (dosFile)
                {
                    data = ExportReader(FilePath, true);
                }
            }
            return data;
        }
        public static IEnumerable<string> GenericRead(string FilePath)
        {
            if (File.Exists(FilePath))
            {
                using (StreamReader sr = new StreamReader(FilePath, System.Text.Encoding.GetEncoding(1252)))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null) if (!String.IsNullOrEmpty(line.Trim())) yield return line; 
                }
            }
            else
            {
                Utilities.Log($"File \"{FilePath}\" does not exist.", Datatypes.ErrorType.CSharpError);
                Environment.Exit(-1);
            }
        }
    }
}
