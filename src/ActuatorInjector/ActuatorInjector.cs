using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Management.Endpoint;

[assembly: HostingStartup(typeof(ActuatorInjector.ActuatorInjector))]

namespace ActuatorInjector;
public class ActuatorInjector : IHostingStartup, IStartupFilter
{
    public void Configure(IWebHostBuilder builder)
    {
        Console.WriteLine("-====TEST====-");
        builder.ConfigureServices(services => services.TryAddEnumerable(ServiceDescriptor.Singleton<IStartupFilter>(this)));

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
        // var config = new ConfigurationBuilder().AddEnvironmentVariables().Build();
        // var services = new ServiceCollection();
        // services.AddSingleton<IConfiguration>(config);
        // var actuatorRegistrations = services.AddAllActuators().Where(x => x.ServiceType != typeof(IConfiguration)).ToList();
        //
        // builder.ConfigureServices(c =>
        // {
        //     foreach (var serviceDescriptor in actuatorRegistrations)
        //     {
        //         Console.WriteLine($"{serviceDescriptor.ServiceType} {serviceDescriptor.ImplementationType} {serviceDescriptor.Lifetime}");
        //         c.Add(serviceDescriptor);
        //     }
        // });
    }

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapAllActuators(null);
            });
            next(app);
        };
    }
}