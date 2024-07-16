using System.Runtime.CompilerServices;
using CloudFoundry.Buildpack.V2.Build;
using Nuke.Common;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;

[assembly: InternalsVisibleTo("SteeltoeBuildpackTests")]
[UnsetVisualStudioEnvironmentVariables]
partial class Build : NukeBuild, 
    IAssemblyInject,
    IPublishBuildpack, 
    IReleaseGithub,
    IMakeOfflineBuildpack
{
    [Solution] public Solution Solution { get; set; } = null!;
    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";
    
    [Parameter("Injection Project")]
    public string? InjectionProject => "SteeltoeBuildpackHostingStartup";
    public string BuildpackProjectName => "SteeltoeBuildpack";


    static Build()
    {
        Environment.SetEnvironmentVariable("NUKE_TELEMETRY_OPTOUT", "true");
    }

    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main () => Execute<Build>(x => ((IPublishBuildpack)x).PublishBuildpack);



    // uncomment below to extend build process
    
    // Target PublishBuildpack => _ => _
    //     .DependsOn<IPublishBuildpack>(x => x.PublishBuildpack)
    //     .DependsOn<IAssemblyInject>(x => x.BuildHttpModule)
    //     .Executes(() =>
    //     {
    //         
    //     });
}
