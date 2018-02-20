
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using TicketingWeb.Models;
using System;
using Ticketing.Web;

namespace TicketingWeb
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
            services.AddMvc()
                    .AddSessionStateTempDataProvider();

            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromSeconds(86400);
            });

            services.AddDbContext<TicketingWebContext>(options =>
                    options.UseSqlServer(Configuration.GetConnectionString("TicketingWebContext")));

            services.AddSingleton(new ConnectionStrings
            {
                ServiceBus = Configuration.GetConnectionString("servicebusConnectionString"),
                StorageQueue = Configuration.GetConnectionString("azurequeueConnectionString")
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseBrowserLink();
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseSession();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Tickets}/{action=Index}/{id?}");
            });

          
        }
    }
}
