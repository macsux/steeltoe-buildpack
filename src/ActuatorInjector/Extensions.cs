using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common;

namespace ActuatorInjector;

public static class Extensions
{
    public static IServiceCollection AddSteeltoeCors(this IServiceCollection services, Action<CorsPolicyBuilder> buildCorsPolicy = null)
        => services.AddCors(setup =>
        {
            setup.AddPolicy("SteeltoeManagement", (policy) =>
            {
                policy.WithMethods("GET", "POST");
                if (Platform.IsCloudFoundry)
                {
                    policy.WithHeaders("Authorization", "X-Cf-App-Instance", "Content-Type", "Content-Disposition");
                }

                if (buildCorsPolicy != null)
                {
                    buildCorsPolicy(policy);
                }
                else
                {
                    policy.AllowAnyOrigin();
                }
            });
        });
}