﻿using System.Linq;

namespace Lifecycle.Supply
{
    class Program
    {
        static int Main(string[] args)
        {
            var argsWithCommand = new[] {"Release"}.Concat(args).ToArray();
            return SteeltoeBuildpack.Program.Main(argsWithCommand);        }
    }
}