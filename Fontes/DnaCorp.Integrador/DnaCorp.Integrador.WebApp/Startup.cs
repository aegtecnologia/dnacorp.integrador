﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DnaCorp.Integrador.Domain.Contratos.Job;
using DnaCorp.Integrador.Infra.MSSql;
using DnaCorp.Integrador.Service.JOB;
using Hangfire;
using Hangfire.Console;
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
        //referencia do hangfire console
        //https://github.com/pieceofsummer/Hangfire.Console/blob/master/README.md

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
            services.AddHangfire(x => 
            {
                x.UseSqlServerStorage(provider);
                x.UseConsole();
            });

            services.AddHangfireServer();
            
            //IoC
            //BANCO DE DADOS
            services.AddTransient<IConexao, Conexao>();
            ////JOBS
            ////AUTOTRAC
            services.AddTransient<IObterVeiculosAutotracJobService, ObterVeiculosAutotracJobService>();
            services.AddTransient<IObterPosicoesAutotracJobService, ObterPosicoesAutotracJobService>();
            ////JABUR
            services.AddTransient<IObterVeiculosJaburJobService, ObterVeiculosJaburJobService>();
            services.AddTransient<IObterPosicoesJaburJobService, ObterPosicoesJaburJobService>();
            ////SASCAR
            services.AddTransient<IObterVeiculosSascarJobService, ObterVeiculosSascarJobService>();
            services.AddTransient<IObterPosicoesSascarJobService, ObterPosicoesSascarJobService>();

            services.AddTransient<ITesteJobService, TesteJobService>();
           
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            //https://crontab.guru/	

            app.UseHangfireServer(options: new BackgroundJobServerOptions
            {
                Queues = new[] { "automacao"}
            });

            const string monitorPath = @"/monitor";
            app.UseHangfireDashboard(monitorPath);

            //AUTOTRAC
            //RecurringJob.AddOrUpdate<IObterVeiculosAutotracJobService>("Autotrac - veiculos", t => t.Executa(), cronExpression: "*/10 * * * *", timeZone: TimeZoneInfo.Local, queue: "automacao");
            //RecurringJob.AddOrUpdate<IObterPosicoesAutotracJobService>("Autotrac - posições", t => t.Executa(), cronExpression: "*/5 * * * *", timeZone: TimeZoneInfo.Local, queue: "automacao");
            //JABUR
            //RecurringJob.AddOrUpdate<IObterVeiculosJaburJobService>("Jabur - veiculos", t => t.Executa(), cronExpression: "* */12 * * *", timeZone: TimeZoneInfo.Local, queue: "automacao");
            //RecurringJob.AddOrUpdate<IObterPosicoesJaburJobService>("Jabur - posições", t => t.Executa(), cronExpression: "*/5 * * * *", timeZone: TimeZoneInfo.Local, queue: "automacao");
            ////SASCAR
            //RecurringJob.AddOrUpdate<IObterVeiculosSascarJobService>("Sascar - veiculos", t => t.Executa(), cronExpression: "* */12 * * *", timeZone: TimeZoneInfo.Local, queue: "automacao");
            //RecurringJob.AddOrUpdate<IObterPosicoesSascarJobService>("Sascar - posições", t => t.Executa(), cronExpression: "*/5 * * * *", timeZone: TimeZoneInfo.Local, queue: "automacao");

            //TESTE
            RecurringJob.AddOrUpdate<ITesteJobService>("Teste", t => t.Executa(null), cronExpression: "*/2 * * * *", timeZone: TimeZoneInfo.Local, queue: "automacao");

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
