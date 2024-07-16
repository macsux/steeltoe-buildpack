using CloudFoundry.Buildpack.V2.Testing;

namespace SteeltoeBuildpack.Tests;

[CollectionDefinition(nameof(ContainerPlatform.Windows))]
public class WindowsTestCollection : ICollectionFixture<WindowsStackFixture>
{
    
}

[CollectionDefinition(nameof(ContainerPlatform.Linux))]
public class LinuxTestCollection : ICollectionFixture<CfLinuxfs3StackFixture>, ICollectionFixture<CfLinuxfs4StackFixture>
{
    
}