using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using NMica.Utils.IO;

namespace SteeltoeBuildpack
{
    public class SteeltoeBuildpack : FinalBuildpack //SupplyBuildpack 
    {
        public override bool Detect(string buildPath)
        {
            return false;
        }

        protected override void Apply(string buildPath, string cachePath, string depsPath, int index)
        {
            Console.WriteLine($"===Applying Steeltoe Buildpack ===");
            var myDependenciesDirectory = (AbsolutePath)depsPath / index.ToString(); // store any runtime dependencies not belonging to the app in this directory
            var buildpackRootDirectory = ((AbsolutePath)Assembly.GetExecutingAssembly().Location).Parent;
            var buildpackLibDirectory = buildpackRootDirectory / "lib";
            
            FileSystemTasks.CopyDirectoryRecursively(buildpackLibDirectory, myDependenciesDirectory);
            EnvironmentalVariables["ASPNETCORE_HOSTINGSTARTUPASSEMBLIES"] = "ActuatorInjector";
            EnvironmentalVariables["DOTNET_SHARED_STORE"] = myDependenciesDirectory / "store";
            EnvironmentalVariables["DOTNET_ADDITIONAL_DEPS"] = myDependenciesDirectory / "deps";
            
            Console.WriteLine($"===Applying {nameof(SteeltoeBuildpack)}===");
            
            
        }
/*
        protected override void PreStartup(string buildPath, string depsPath, int index)
        {
            Console.WriteLine("Application is about to start...");
            EnvironmentalVariables["MY_SETTING"] = "value"; // can set env vars before app starts running
        }
*/
        public override string GetStartupCommand(string buildPath)
        {
            return "test.exe";
        }
    }
}
