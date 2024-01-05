using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using NMica.Utils.IO;

namespace SteeltoeBuildpack
{
    public class SteeltoeBuildpack : SupplyBuildpack 
    {
        public override void Finalize(string buildPath, string cachePath, string depsPath, int index)
        {
            Console.WriteLine($"=== Finalize ===");
        }

        public override void Release(string buildPath)
        {
            Console.WriteLine($"=== Release ===");
        }

        protected override void Apply(string buildPath, string cachePath, string depsPath, int index)
        {
            Console.WriteLine($"===Applying {nameof(SteeltoeBuildpack)}===");
            var myDependenciesDirectory = (AbsolutePath)depsPath / index.ToString(); // store any runtime dependencies not belonging to the app in this directory
            var buildpackRootDirectory = ((AbsolutePath)Assembly.GetExecutingAssembly().Location).Parent.Parent;
            var buildpackLibDirectory = buildpackRootDirectory / "lib";

            // Console.WriteLine($"Root: {buildpackRootDirectory} ; {Directory.Exists(buildpackRootDirectory)}");
            // Console.WriteLine($"To: {myDependenciesDirectory} ; {Directory.Exists(myDependenciesDirectory)}");
            // Console.WriteLine($"Lib: {buildpackLibDirectory} ; {Directory.Exists(buildpackLibDirectory)}");
            FileSystemTasks.EnsureExistingDirectory(myDependenciesDirectory);
            // Console.WriteLine(Directory.Exists(buildpackLibDirectory));
            // Console.WriteLine(Directory.Exists(myDependenciesDirectory));
            FileSystemTasks.CopyDirectoryRecursively(buildpackLibDirectory, myDependenciesDirectory, DirectoryExistsPolicy.Merge);
            FileSystemTasks.CopyFile($"{buildpackLibDirectory}/store/x64/net8.0/actuatorinjector/1.0.0/ActuatorInjector.dll", $"{buildPath}/ActuatorInjector.dll" );
            EnvironmentalVariables["ASPNETCORE_HOSTINGSTARTUPASSEMBLIES"] = "ActuatorInjector";
            // EnvironmentalVariables["DOTNET_SHARED_STORE"] = $"$DEPS_DIR/{index}/store";
            EnvironmentalVariables["DOTNET_SHARED_STORE"] = $"/home/vcap/deps/{index}/store";
            EnvironmentalVariables["DOTNET_ADDITIONAL_DEPS"] = $"/home/vcap/deps/{index}/deps/ActuatorInjector.deps.json";
            
            
            
            
        }
/*
        protected override void PreStartup(string buildPath, string depsPath, int index)
        {
            Console.WriteLine("Application is about to start...");
            EnvironmentalVariables["MY_SETTING"] = "value"; // can set env vars before app starts running
        }
*/
 
    }
}
