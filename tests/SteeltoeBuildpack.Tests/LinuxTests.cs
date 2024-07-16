using CloudFoundry.Buildpack.V2.Testing;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using static CloudFoundry.Buildpack.V2.Testing.DirectoryHelper;

namespace SteeltoeBuildpack.Tests;

[Collection(nameof(ContainerPlatform.Linux))]
public class LinuxTests(ITestOutputHelper output, CfLinuxfs4StackFixture fixture) : BaseTests(output, fixture)
{
    [Fact]
    public async Task TestSteeltoeActuators()
    {
        var appDir = RootDirectory / "tests" / "fixtures" / "dotnetapp";
        var stagingContext = _fixture.CreateStagingContext(appDir);
        stagingContext.Buildpacks.Add( RootDirectory / "artifacts" / "latest" / "linux-x64" / "buildpack.zip");
        stagingContext.Buildpacks.Add(RootDirectory / "artifacts" / "dotnet-core-buildpack.zip");
        stagingContext.SkipDetect = true;
        var stageResult = await _fixture.Stage(stagingContext, _output);

        var dropletDir = stageResult.DropletDirectory;
        var context = _fixture.CreateLaunchContext(dropletDir);
        var launchResult = await _fixture.Launch(context, _output);
        var result2 = await launchResult.HttpClient.GetAsync("/actuator");
        result2.IsSuccessStatusCode.Should().BeTrue();
    }
}