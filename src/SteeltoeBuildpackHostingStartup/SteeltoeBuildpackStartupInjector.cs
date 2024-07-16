using System.Net;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using Steeltoe.Management.Endpoint;
using SteeltoeBuildpackHostingStartup;

[assembly: HostingStartup(typeof(SteeltoeBuildpackStartupInjector))]

namespace SteeltoeBuildpackHostingStartup;

public class SteeltoeBuildpackStartupInjector: IHostingStartup
{
    public void Configure(IWebHostBuilder builder)
    {
        Console.WriteLine("Adding Steeltoe");
        builder.AddCloudFoundryConfiguration();
        builder.AddAllActuators();

    }

}