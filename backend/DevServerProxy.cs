using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Forwarder;

namespace Csm.PixelGrove;

internal static class DevServerProxy
{
    public static WebApplicationBuilder AddDevServerProxy(this WebApplicationBuilder builder)
    {
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                if (builder.Configuration["Dev:Web:BrowserUrl"] is { } browserUrl) policy.WithOrigins(browserUrl);
                if (builder.Configuration["Dev:Web:ServerUrl"] is { } serverUrl) policy.WithOrigins(serverUrl);
                policy.AllowCredentials().AllowAnyHeader().AllowAnyMethod();
            });
        });

        builder.Services.AddReverseProxy().LoadFromMemory([
            new RouteConfig
            {
                RouteId = "devServer",
                ClusterId = "devCluster",
                Match = new RouteMatch { Path = "{**catchall}" },
            },
        ], [
            new ClusterConfig
            {
                ClusterId = "devCluster",
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    {
                        "default",
                        new DestinationConfig
                        {
                            Address = builder.Configuration["Dev:Web:ServerUrl"] ?? "http://localhost:3001",
                        }
                    },
                },
                HttpRequest = new ForwarderRequestConfig { ActivityTimeout = TimeSpan.MaxValue },
            },
        ]);

        return builder;
    }

    public static WebApplication UseDevServerProxy(this WebApplication app)
    {
        app.UseCors();
        app.MapReverseProxy();
        return app;
    }
}
