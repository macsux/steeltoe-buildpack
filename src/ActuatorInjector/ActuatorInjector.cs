using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.CloudFoundry;

[assembly: HostingStartup(typeof(ActuatorInjector.ActuatorInjector))]

namespace ActuatorInjector;
public class ActuatorInjector : IHostingStartup, IStartupFilter
{
    public void Configure(IWebHostBuilder builder)
    {
        Console.WriteLine("Injecting Steeltoe");
        builder.ConfigureServices(services => services.TryAddEnumerable(ServiceDescriptor.Singleton<IStartupFilter>(this)));

        builder.ConfigureServices(c => c.AddSteeltoeCors());
        builder.ConfigureAppConfiguration(c => c.AddCloudFoundry());
        builder.AddHypermediaActuator();
        builder.AddThreadDumpActuator();
        builder.AddHeapDumpActuator();
        builder.AddDbMigrationsActuator();
        builder.AddEnvActuator();
        builder.AddInfoActuator();
        builder.AddHealthActuator();
        builder.AddLoggersActuator();
        builder.AddTraceActuator();
        builder.AddMappingsActuator();
        builder.AddCloudFoundryActuator();
        builder.AddAllActuators();
    }

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            app.UseRouting();
            if (app.ApplicationServices.GetService<ICorsService>() != null)
            {
                app.UseCors("SteeltoeManagement");
            }
            app.UseCloudFoundrySecurity();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapAllActuators(null);
            });
            next(app);
        };
    }


    
}