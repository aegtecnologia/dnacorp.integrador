using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DnaCorp.Integrador.Domain.Contratos.Job;
using DnaCorp.Integrador.Infra.MSSql;
using DnaCorp.Integrador.Service.JOB;
using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace DnaCorp.Integrador.WebApp
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
            var configuration = services.BuildServiceProvider().GetService<IConfiguration>();
            var provider = configuration.GetConnectionString("DefaultConnection");

            services.Configure<IISOptions>(o =>
            {
                o.ForwardClientCertificate = false;
            });

            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            //hangfire
            GlobalJobFilters.Filters.Add(new DisableConcurrentExecutionAttribute(timeoutInSeconds: 60));
            services.AddHangfire(x => x.UseSqlServerStorage(provider));
            services.AddHangfireServer();
            
            //IoC
            services.AddTransient<IConexao, Conexao>();
            services.AddTransient<IObterVeiculosJaburJobService, ObterVeiculosJaburJobService>();
            services.AddTransient<IObterVeiculosSascarJobService, ObterVeiculosSascarJobService>();
            services.AddTransient<IObterPosicoesJaburJobService, ObterPosicoesJaburJobService>();
            services.AddTransient<IObterPosicoesSascarJobService, ObterPosicoesSascarJobService>();


            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseHangfireServer(options: new BackgroundJobServerOptions
            {
                Queues = new[] { "automacao"}
            });

            const string monitorPath = @"/monitor";
            app.UseHangfireDashboard(monitorPath);

            //RecurringJob.AddOrUpdate<IObterVeiculosJaburJobService>("Obter Veiculos Jabur", t => t.Executa(), cronExpression: "*/45 * * * *", timeZone: TimeZoneInfo.Local, queue: "automacao");
            RecurringJob.AddOrUpdate<IObterVeiculosSascarJobService>("Obter Veiculos Sascar", t => t.Executa(), cronExpression: "*/45 * * * *", timeZone: TimeZoneInfo.Local, queue: "automacao");
            //RecurringJob.AddOrUpdate<IObterPosicoesJaburJobService>("Obter Posições Jabur", t => t.Executa(), cronExpression: "*/30 * * * *", timeZone: TimeZoneInfo.Local, queue: "automacao");
            //RecurringJob.AddOrUpdate<IObterPosicoesSascarJobService>("Obter Posições Sascar", t => t.Executa(), cronExpression: "*/30 * * * *", timeZone: TimeZoneInfo.Local, queue: "automacao");
           
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });

        }
    }
}
