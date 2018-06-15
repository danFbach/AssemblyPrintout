using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace AssemblyPrintout
{
    class Read
    {
        public List<string> rafRead(string path)
        {
            path = @"C:\INVEN\NVEND.DAT";
            List<string> data = new List<string>();
            int recordLength = 574;
            //FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 2, FileOptions.RandomAccess);
            //BinaryReader bn = new BinaryReader(fs);
            //for (int i = 1; i <= (fs.Length / recordLength); i++)
            //{
            //    fs.Seek(i * recordLength, SeekOrigin.Begin);
            //    data.Add(bn.ReadString());
            //}

            using (FileStream _fs = File.Open(path, FileMode.Open))
            {
                byte[] b = new byte[574];
                byte[] n = new byte[4];
                UTF8Encoding t = new UTF8Encoding(true);
                while (_fs.Read(b, 0, b.Length) > 0)
                {
                    string x = t.GetString(b);
                    string num = t.GetString(n);
                    //CVI c = new CVI();
                    data.Add(x);
                }
            }
            
            return data;
        }
    //    FIELD #9,4 AS AB$(0),6 AS AB$(1),35 AS AB$(2),24 AS AB$(3),24 AS AB$(4),30 AS AB$(5),20 AS AB$(6),4 AS AB$(7),30 AS AB$(8),45 AS AB$(9),45 AS AB$(10), _
    //20 AS AB$(11),30 AS AB$(12),45 AS AB$(13),20 AS AB$(14),30 AS AB$(15),45 AS AB$(16),20 AS AB$(17),30 AS AB$(18),45 AS AB$(19),20 AS AB$(20)
    //    AAB$(0) = "V-ID": AAB$(1) = "VCODE": AAB$(2) = " COMPANY NAME": AAB$(3) = " ADDRESS":
    //    AAB$(4) = " ADDRESS 2": AAB$(5) = " CITY, STATE, ZIP": AAB$(6) = " FAX": AAB$(7) = " TERMS": AAB$(8) = " ORDER CONTACT": AAB$(9) = " ORDER EMAIL":
    //    AAB$(10) = " ORDER CC EMAIL": AAB$(11) = " ORDER PHONE": AAB$(12) = " ACCOUNT CONTACT": AAB$(13) = " ACCOUNT EMAIL":
    //    AAB$(14) = " ACCOUNT PHONE": AAB$(15) = " QUALITY CONTACT": AAB$(16) = " QUALITY EMAIL": AAB$(17) = " QUALITY PHONE":
    //    AAB$(18) = " SHIPPING CONTACT": AAB$(19) = " SHIPPING EMAIL": AAB$(20) = " SHIPPING PHONE"
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
