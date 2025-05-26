using System;
using System.Collections.Generic;
using System.Fabric;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ServiceFabric.Services.Communication.AspNetCore;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Microsoft.ServiceFabric.Data;

namespace ExchangeDataAPI
{
    /// <summary>
    /// The FabricRuntime creates an instance of this class for each service type instance.
    /// </summary>
    internal sealed class ExchangeDataAPI : StatelessService
    {
        public ExchangeDataAPI(StatelessServiceContext context)
            : base(context)
        { }

        /// <summary>
        /// Optional override to create listeners (like tcp, http) for this service instance.
        /// </summary>
        /// <returns>The collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return new ServiceInstanceListener[]
            {
                new ServiceInstanceListener(serviceContext =>
                    new KestrelCommunicationListener(serviceContext, "ServiceEndpoint", (url, listener) =>
                    {
                        ServiceEventSource.Current.ServiceMessage(serviceContext, $"Starting Kestrel on {url}");

                        var builder = WebApplication.CreateBuilder();

                        builder.Services.AddSingleton<StatelessServiceContext>(serviceContext);
                        builder.WebHost
                                    .UseKestrel()
                                    .UseContentRoot(Directory.GetCurrentDirectory())
                                    .UseServiceFabricIntegration(listener, ServiceFabricIntegrationOptions.None)
                                    .UseUrls(url);
                        builder.Services.AddControllers();
                        builder.Services.AddOpenApi();

                        builder.Services.AddCors(options =>
                        {
                         options.AddPolicy("AllowAngularApp",
                         policy => policy.WithOrigins("http://localhost:4200")
                         .AllowAnyHeader()
                         .AllowAnyMethod());
                        });

                        builder.Services.AddCors(options =>
                        {
                         options.AddPolicy("AllowAngularApp",
                         policy => policy.WithOrigins("http://localhost:8330")
                         .AllowAnyHeader()
                         .AllowAnyMethod());
                        });

                        var app = builder.Build();
                        app.UseCors("AllowAngularApp");

                        if (app.Environment.IsDevelopment())
                        {
                        app.MapOpenApi();
                        }
                        app.UseAuthorization();
                        app.MapControllers();
                        
                        return app;

                    }))
            };
        }
    }
}
