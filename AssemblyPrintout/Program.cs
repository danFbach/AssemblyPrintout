﻿namespace AssemblyPrintout
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0) ///if opened with arguments, run normally
            {
                Util.InDevMode(args[args.Length - 1] == "-debug");
                Menu.ParseArgs(args);
            }
        }
    }
}