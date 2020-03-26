using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitCommunicationLib.Interfaces;
using RabbitCommunicationLib.Producer;
using RabbitCommunicationLib.Queues;
using RabbitCommunicationLib.TransferModels;

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
            services.Configure<FormOptions>(x =>
            {
                x.MultipartBodyLengthLimit = 536870900;
            });

            services.AddControllers()
                .AddNewtonsoftJson(x => x.UseMemberCasing());

            services.AddLogging(options =>
            {
                options.AddConsole(o =>
                {
                    o.TimestampFormat = "[yyyy-MM-dd HH:mm:ss zzz] ";
                });
            });


            services.AddApiVersioning();


            var AMQP_URI = Configuration.GetValue<string>("AMQP_URI") ?? throw new ArgumentNullException("Environment variable AMQP_URI is not set!");
            var AMQP_UPLOAD_RECEIVED_QUEUE = Configuration.GetValue<string>("AMQP_UPLOAD_RECEIVED_QUEUE") ?? throw new ArgumentNullException("Environment variable AMQP_UPLOAD_RECEIVED_QUEUE is not set!");
            var demoCentralConnection = new QueueConnection(AMQP_URI, AMQP_UPLOAD_RECEIVED_QUEUE);

            string BLOB_CONNECTION_STRING = Configuration.GetValue<string>("BLOB_CONNECTION_STRING") ?? throw new ArgumentNullException("Environment variable BLOB_CONNECTION_STRING is not set!");


            services.AddTransient<IBlobStorage, BlobStorage>(factory =>
            {
                return new BlobStorage(
                    BLOB_CONNECTION_STRING,
                    factory.GetRequiredService<ILogger<BlobStorage>>());
            });

            services.AddSingleton<IProducer<DemoInsertInstruction>>(factory =>
            {
                return new Producer<DemoInsertInstruction>(demoCentralConnection);
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

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

        }
    }
}
