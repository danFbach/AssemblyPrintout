using System.Linq;

namespace AssemblyPrintout
{
	class Program
	{
		static void Main(string[] args)
		{
			mainSwitch ms = new mainSwitch( );
			if(args.Count( ) > 0) ///if opened with arguments, run normally
			{
				ms._switch(args);
			}
			else ///if no arguments, run with dummy args
			{
				string[] _args = { "-p" };  ///DUMMY ARGUMENTS
				ms._switch(_args);
			}

		}
	}
}