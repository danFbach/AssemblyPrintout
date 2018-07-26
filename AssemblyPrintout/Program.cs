namespace AssemblyPrintout
{
	class Program
	{
		static void Main(string[] args)
		{
			mainSwitch ms = new mainSwitch( );
			string[] _args = { "-p" };  ///DUMMY ARGUMENTS
			//ms._switch(args);

			ms._switch(_args);
		}
	}
}