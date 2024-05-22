using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using VaquinhaAnimal.Api.Configuration;
using VaquinhaAnimal.Api.Configurations;
using VaquinhaAnimal.Data.Context;
using VaquinhaAnimal.Domain.Interfaces;

namespace VaquinhaAnimal.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<VaquinhaDbContext>(options =>
            {
                options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"));
            });

            services.AddIdentityConfiguration(Configuration);
            services.AddAutoMapper(typeof(Startup));
            services.WebApiConfig();
            services.AddSwaggerConfig();
            services.AddLoggingConfig(Configuration);
            services.ResolveDependencies();
            services.AddSignalR();
            //services.AddHangfire(op => op.UseMemoryStorage());
            //services.AddHangfireServer();
        }

        //public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IApiVersionDescriptionProvider provider, IBackgroundJobClient backgroundJobs)
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IApiVersionDescriptionProvider provider)
        {
            app.UseApiConfig(env);
            app.UseSwaggerConfig(provider);
            //app.UseLoggingConfiguration();
            //app.UseHangfireDashboard();
            //RecurringJob.AddOrUpdate(() => Console.WriteLine(""), Cron.Minutely);
            //RecurringJob.AddOrUpdate<IHangfireController>(x => x.ObterCampanha(), Cron.Minutely);
        }
    }
}
