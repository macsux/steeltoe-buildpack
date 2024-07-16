using CloudFoundry.Buildpack.V2;
using CloudFoundry.Buildpack.V2.SteeltoeBuildpack;

return BuildpackHost.Create<SteeltoeBuildpackBuildpack>().Run();