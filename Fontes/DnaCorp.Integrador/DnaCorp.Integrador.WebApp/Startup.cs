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
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddHangfire(x => x.UseSqlServerStorage("Data Source=IFTBSNBKL087402;Initial Catalog=db_aegtecnologia;Persist Security Info=False;User ID=admin; Password = Inter@2019"));
            services.AddHangfireServer();

            //IoC
            services.AddTransient<IConexao, Conexao>();
            services.AddTransient<IObterEspelhamentoJaburJobService, ObterEspelhamentoJaburJobService>();


            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            //hangfire
            app.UseHangfireDashboard(@"/monitor");

            RecurringJob.AddOrUpdate<IObterEspelhamentoJaburJobService>("Obter espelhamentos Jabur", t => t.Executa(), cronExpression: "*/45 * * * *", timeZone: TimeZoneInfo.Local, queue: "automacao");
            RecurringJob.AddOrUpdate<IObterPosicoesJaburJobService>("Obter posicoes Jabur", t => t.Executa(), cronExpression: "*/15 * * * *", timeZone: TimeZoneInfo.Local, queue: "automacao");


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
