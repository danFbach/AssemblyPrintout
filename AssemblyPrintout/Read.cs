using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;

namespace AssemblyPrintout
{
	class Read
	{
		string _ = Environment.NewLine;
		public List<string> reader(string path)
		{
			List<string> data = new List<string>( );
			if(File.Exists(path))
			{
				using(StreamReader sr = new StreamReader(path))
				{
					string line;
					while((line = sr.ReadLine( )) != null)
					{
						if(!String.IsNullOrEmpty(line.Trim( )))
						{
							data.Add(line);
						}
					}
					sr.Close( );
				}
			}
			return data;
		}
		public List<string> genericRead(string fileLoc)
		{
			List<string> data = new List<string>( );
			try
			{
				if(File.Exists(fileLoc))
				{
					using(StreamReader sr = new StreamReader(fileLoc))
					{
						string line;
						while((line = sr.ReadLine( )) != null)
						{
							if(!String.IsNullOrEmpty(line.Trim( )))
							{
								data.Add(line);
							}
						}
					}
					return data;
				}
				else
				{
					using(StreamWriter sw = new StreamWriter(@"C:\inven\cSharpError.txt"))
					{
						sw.WriteLine("File \"" + fileLoc + "\" does not exist.");
					}
					Process.Start("Notepad.exe", @"C:\inven\cSharpError.txt");
					return null;
				}
			}
			catch(Exception e)
			{
				using(StreamWriter sw = new StreamWriter(@"C:\inven\cSharpError.txt"))
				{
					sw.WriteLine("Read Error." + _ + e.Message + _ + e.InnerException + _ + e.StackTrace);
				}
				Process.Start("Notepad.exe", @"C:\inven\cSharpError.txt");
				return null;
			}
		}
	}
}
