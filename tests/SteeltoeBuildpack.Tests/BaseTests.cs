using CloudFoundry.Buildpack.V2.Testing;

namespace SteeltoeBuildpack.Tests;

public abstract class BaseTests
{
    protected readonly ITestOutputHelper _output;
    protected readonly ContainersPlatformFixture _fixture;

    protected BaseTests(ITestOutputHelper output, ContainersPlatformFixture fixture)
    {
        _output = output;
        _fixture = fixture;
        fixture.OutputStream = new TestOutputStream(output);
    }
}