using System;
using ManualUpload.Communication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitCommunicationLib.Queues;


namespace ManualUpload
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            services.AddLogging(o =>
            {
                o.AddConsole();
                o.AddDebug();
            });


            services.AddApiVersioning(o => {
                o.ReportApiVersions = true;
                o.AssumeDefaultVersionWhenUnspecified = true;
                o.DefaultApiVersion = new ApiVersion(1, 0);
            });

            var AMQP_URI = Configuration.GetValue<string>("AMQP_URI") ?? throw new ArgumentNullException("Environment variable AMQP_URI is not set!");
            var AMQP_UPLOAD_RECEIVED_QUEUE = Configuration.GetValue<string>("AMQP_UPLOAD_RECEIVED_QUEUE") ?? throw new ArgumentNullException("Environment variable AMQP_UPLOAD_RECEIVED_QUEUE is not set!");
            var demoCentralConnection = new QueueConnection(AMQP_URI, AMQP_UPLOAD_RECEIVED_QUEUE);

            services.AddHostedService<IDemoCentral>(services =>
            {
                return new DemoCentral(demoCentralConnection);
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
