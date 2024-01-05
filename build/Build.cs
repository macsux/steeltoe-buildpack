using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nuke.Common;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitHub;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Tools.NerdbankGitVersioning;
using Nuke.Common.Utilities.Collections;
using Octokit;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using FileMode = System.IO.FileMode;
using ZipFile = System.IO.Compression.ZipFile;

[assembly: InternalsVisibleTo("SteeltoeBuildpackTests")]
class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    [Flags]
    public enum StackType
    {
        Windows = 1,
        Linux = 2
    }
    public static int Main () => Execute<Build>(x => x.Publish);
    const string BuildpackProjectName = "SteeltoeBuildpack";
    string GetPackageZipName(string runtime) => $"{BuildpackProjectName}-{runtime}-{GitVersion.SemVer2}.zip";

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;
    
    [Parameter("Target CF stack type - 'windows' or 'linux'. Determines buildpack runtime (Framework or Core). Default is Linux")]
    readonly StackType Stack = StackType.Linux;
    
    [Parameter("GitHub personal access token with access to the repo")]
    string GitHubToken;

    [Parameter("Application directory against which buildpack will be applied")]
    readonly string ApplicationDirectory;
    
    // AbsolutePath StoreDirectory => ArtifactsDirectory / "store";
    Nuke.Common.ProjectModel.Project InjectorProject => Solution.GetAllProjects("ActuatorInjector").FirstOrDefault();


    IEnumerable<PublishTarget> PublishCombinations
    {
        get
        {
            if (Stack.HasFlag(StackType.Windows))
                yield return new PublishTarget {Framework = "net472", Runtime = "win-x64"};
            if (Stack.HasFlag(StackType.Linux))
                yield return new PublishTarget {Framework = "net8.0", Runtime = "linux-x64"};
        }
    }

    [Solution] readonly Solution Solution;
    [GitRepository] readonly GitRepository GitRepository;
    [NerdbankGitVersioning] readonly NerdbankGitVersioning GitVersion;

    AbsolutePath SourceDirectory => RootDirectory / "src";
    AbsolutePath TestsDirectory => RootDirectory / "tests";
    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";
    
    string[] LifecycleHooks = {"detect", "supply", "release", "finalize"};

    Target Clean => _ => _
        .Description("Cleans up **/bin and **/obj folders")
        .Executes(() =>
        {
            SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
            TestsDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
        });

    Target Compile => _ => _
        .Description("Compiles the buildpack")
        .DependsOn(Clean)
        .Executes(() =>
        {
            
            Logger.Info(Stack);
            DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                
                .SetAssemblyVersion(GitVersion.AssemblyVersion)
                .SetFileVersion(GitVersion.AssemblyFileVersion)
                .SetInformationalVersion(GitVersion.AssemblyInformationalVersion)
                .CombineWith(PublishCombinations, (c, p) => c
                    //.AddProperty("-p:TargetArch",p.Runtime)
                    .SetFramework(p.Framework)
                    // .SetRuntime(p.Runtime))
            ));
        });
    
    Target Publish => _ => _
        .Description("Packages buildpack in Cloud Foundry expected format into /artifacts directory")
        // .DependsOn(Clean)
        .Executes(() =>
        {
            foreach (var publishCombination in PublishCombinations)
            {
                var framework = publishCombination.Framework;
                var runtime = publishCombination.Runtime;
                var packageZipName = GetPackageZipName(runtime);
                var workDirectory = TemporaryDirectory / "work"; 
                var packDirectory = workDirectory / "pack";
                workDirectory.CreateOrCleanDirectory();
                
                
                var buildpackProject = Solution.GetAllProjects(BuildpackProjectName).FirstOrDefault();
                if(buildpackProject == null)
                    throw new Exception($"Unable to find project called {BuildpackProjectName} in solution {Solution.Name}");
                var publishDirectory = buildpackProject.Directory / "bin" / Configuration / framework / runtime / "publish";
                var workBinDirectory = packDirectory / "bin";
                var workLibDirectory = packDirectory / "lib";
                var storeDirectory = workLibDirectory / "store";
                var compileDir = InjectorProject.Directory / "bin" / "Debug" / framework;
                var injectorDeps = compileDir / $"{InjectorProject.Name}.deps.json";
                
                CopyFile(injectorDeps, workLibDirectory / "deps" / injectorDeps.Name);
                BuildStore(framework, storeDirectory, injectorDeps);

                DotNetPublish(s => s
                    .SetProject(Solution)
                    .SetConfiguration(Configuration)
                    .SetFramework(framework)
                    .SetRuntime(runtime)
                    .SetAssemblyVersion(GitVersion.AssemblyVersion)
                    .SetFileVersion(GitVersion.AssemblyFileVersion)
                    .SetInformationalVersion(GitVersion.AssemblyInformationalVersion)
                );

                var lifecycleBinaries = Solution.GetAllProjects("Lifecycle*")
                    .Select(x => x.Directory / "bin" / Configuration / framework / runtime / "publish")
                    .SelectMany(x => Directory.GetFiles(x).Where(path => LifecycleHooks.Any(hook => Path.GetFileName(path).StartsWith(hook))));

                foreach (var lifecycleBinary in lifecycleBinaries)
                {
                    CopyFileToDirectory(lifecycleBinary, workBinDirectory, FileExistsPolicy.OverwriteIfNewer);
                }

                CopyDirectoryRecursively(publishDirectory, workBinDirectory, DirectoryExistsPolicy.Merge);
                var tempZipFile = workDirectory / packageZipName;

                ZipFile.CreateFromDirectory(packDirectory, tempZipFile, CompressionLevel.NoCompression, false);
                MakeFilesInZipUnixExecutable(tempZipFile);
                CopyFileToDirectory(tempZipFile, ArtifactsDirectory, FileExistsPolicy.Overwrite);
                Logger.Block(ArtifactsDirectory / packageZipName);
            }
        });


    void BuildStore(string framework, AbsolutePath storeDirectory, AbsolutePath depsFile)
    {
        storeDirectory.CreateOrCleanDirectory();
            AbsolutePath storePackagesDirectory = storeDirectory / "x64" / framework;
            // AbsolutePath depsDirectory = ArtifactsDirectory / "deps";
            // depsDirectory.CreateOrCleanDirectory();
            // var actuatorinjectorDeps = depsDirectory / $"{InjectorProject.Name}.deps.json";
            storeDirectory.CreateOrCleanDirectory();
            
            DotNetBuild(x => x.SetProjectFile(Solution.Path));
            // var compileDir = InjectorProject.Directory / "bin" / "Debug" / framework;
            // var injectorDeps = compileDir / $"{InjectorProject.Name}.deps.json";
            // var injectorDll = compileDir / $"{InjectorProject.Name}.dll";
            var injectorDll = (AbsolutePath)depsFile.ToString().Replace(".deps.json", ".dll");
            var json = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(depsFile));

            // File.WriteAllText(actuatorinjectorDeps, json.ToString());
            
            var requiredStoreItems = json.SelectTokens("targets.*")
                .Cast<JObject>()
                .SelectMany(x => x.Properties())
                .Select(x =>
                {
                    var packageNameSegments = x.Name.Split("/");
                    var packageName = packageNameSegments[0];
                    var version = packageNameSegments[1];
                    return new
                    {
                        Name = packageName,
                        Version = version,
                        Files = x
                            .SelectTokens("..runtime")
                            .Cast<JObject>()
                            .SelectMany(y => y.Properties().Select(z => z.Name))
                            .ToList()
                    };
                })
                .Where(x => x.Files.Any() && x.Name != InjectorProject.Name)
                .ToList();
            
            

            var nugetCacheDirectory = (AbsolutePath)Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) / ".nuget" / "packages";

            var filesToCopy = requiredStoreItems.SelectMany(x => x.Files.Select(f => new
            {
                From = nugetCacheDirectory / x.Name / x.Version / f,
                To = storePackagesDirectory / x.Name.ToLower() / x.Version / f
            }))
            .ToList();
            
            filesToCopy.Add(new
            {
                From = injectorDll,
                To = storePackagesDirectory / InjectorProject.Name.ToLower() / "1.0.0" / injectorDll.Name
            });

            foreach (var file in filesToCopy)
            {
                CopyFile(file.From, file.To);
            }
    }
    
    
    Target Release => _ => _
        .Description("Creates a GitHub release (or amends existing) and uploads buildpack artifact")
        .DependsOn(Publish)
        .Requires(() => GitHubToken)
        .Executes(async () =>
        {
            foreach (var publishCombination in PublishCombinations)
            {
                var runtime = publishCombination.Runtime;
                var packageZipName = GetPackageZipName(runtime);
                if (!GitRepository.IsGitHubRepository())
                    throw new Exception("Only supported when git repo remote is github");
    
                var client = new GitHubClient(new ProductHeaderValue(BuildpackProjectName))
                {
                    Credentials = new Credentials(GitHubToken, AuthenticationType.Bearer)
                };
                var gitIdParts = GitRepository.Identifier.Split("/");
                var owner = gitIdParts[0];
                var repoName = gitIdParts[1];
    
                var releaseName = $"v{GitVersion.SemVer2}";
                Release release;
                try
                {
                    release = await client.Repository.Release.Get(owner, repoName, releaseName);
                }
                catch (NotFoundException)
                {
                    var newRelease = new NewRelease(releaseName)
                    {
                        Name = releaseName,
                        Draft = false,
                        Prerelease = false
                    };
                    release = await client.Repository.Release.Create(owner, repoName, newRelease);
                }
    
                var existingAsset = release.Assets.FirstOrDefault(x => x.Name == packageZipName);
                if (existingAsset != null)
                {
                    await client.Repository.Release.DeleteAsset(owner, repoName, existingAsset.Id);
                }
    
                var zipPackageLocation = ArtifactsDirectory / packageZipName;
                var stream = File.OpenRead(zipPackageLocation);
                var releaseAssetUpload = new ReleaseAssetUpload(packageZipName, "application/zip", stream, TimeSpan.FromHours(1));
                var releaseAsset = await client.Repository.Release.UploadAsset(release, releaseAssetUpload);
    
                Logger.Block(releaseAsset.BrowserDownloadUrl);
            }
        });

    Target Detect => _ => _
        .Description("Invokes buildpack 'detect' lifecycle event")
        .Requires(() => ApplicationDirectory)
        .Executes(() =>
        {
            try
            {
                DotNetRun(s => s
                    .SetProjectFile(Solution.GetProject("Lifecycle.Detect").Path)
                    .SetApplicationArguments(ApplicationDirectory)
                    .SetConfiguration(Configuration)
                    .SetFramework("netcoreapp3.1"));
                Logger.Block("Detect returned 'true'");
            }
            catch (ProcessException)
            {
                Logger.Block("Detect returned 'false'");
            }
        });

    Target Supply => _ => _
        .Description("Invokes buildpack 'supply' lifecycle event")
        .Requires(() => ApplicationDirectory)
        .Executes(() =>
        {
            var home = (AbsolutePath)Path.GetTempPath() / Guid.NewGuid().ToString();
            var app = home / "app";
            var deps = home / "deps";
            var index = 0;
            var cache = home / "cache";
            CopyDirectoryRecursively(ApplicationDirectory, app);

            DotNetRun(s => s
                .SetProjectFile(Solution.GetProject("Lifecycle.Supply").Path)
                .SetApplicationArguments($"{app} {cache} {app} {deps} {index}")
                .SetConfiguration(Configuration)
                .SetFramework("netcoreapp3.1"));
            Logger.Block($"Buildpack applied. Droplet is available in {home}");

        });

    public void MakeFilesInZipUnixExecutable(AbsolutePath zipFile)
    {
        var tmpFileName = zipFile + ".tmp";
        using (var input = new ZipInputStream(File.Open(zipFile, FileMode.Open)))
        using (var output = new ZipOutputStream(File.Open(tmpFileName, FileMode.Create)))
        {
            output.SetLevel(9);
            ZipEntry entry;
		
            while ((entry = input.GetNextEntry()) != null)
            {
                var outEntry = new ZipEntry(entry.Name) {HostSystem = (int) HostSystemID.Unix};
                var entryAttributes =  
                    ZipEntryAttributes.ReadOwner | 
                    ZipEntryAttributes.ReadOther | 
                    ZipEntryAttributes.ReadGroup |
                    ZipEntryAttributes.ExecuteOwner | 
                    ZipEntryAttributes.ExecuteOther | 
                    ZipEntryAttributes.ExecuteGroup;
                entryAttributes = entryAttributes | (entry.IsDirectory ? ZipEntryAttributes.Directory : ZipEntryAttributes.Regular);
                outEntry.ExternalFileAttributes = (int) (entryAttributes) << 16; // https://unix.stackexchange.com/questions/14705/the-zip-formats-external-file-attribute
                output.PutNextEntry(outEntry);
                input.CopyTo(output);
            }
            output.Finish();
            output.Flush();
        }

        DeleteFile(zipFile);
        RenameFile(tmpFileName,zipFile, FileExistsPolicy.Overwrite);
    }
    
    [Flags]
    enum ZipEntryAttributes
    {
        ExecuteOther = 1,
        WriteOther = 2,
        ReadOther = 4,
	
        ExecuteGroup = 8,
        WriteGroup = 16,
        ReadGroup = 32,

        ExecuteOwner = 64,
        WriteOwner = 128,
        ReadOwner = 256,

        Sticky = 512, // S_ISVTX
        SetGroupIdOnExecution = 1024,
        SetUserIdOnExecution = 2048,

        //This is the file type constant of a block-oriented device file.
        NamedPipe = 4096,
        CharacterSpecial = 8192,
        Directory = 16384,
        Block = 24576,
        Regular = 32768,
        SymbolicLink = 40960,
        Socket = 49152
	
    }
    class PublishTarget
    {
        public string Framework { get; set; }
        public string Runtime { get; set; }
    }
}
