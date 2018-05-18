using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Session;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using CutfloSMSAuth.Models;

namespace CutfloSMSAuth
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
            SqlDebugger debugger = SqlDebugger.Instance;

            //Add Database Connections
            services.AddDbContext<UserContext>(opt => opt.UseInMemoryDatabase("Users"));
            services.Add(new ServiceDescriptor(typeof(UserSqlContext), new UserSqlContext(debugger)));

            //Establish API Routes
            services.AddRouting();

            services.AddMvc();

            //Enable Session Dependenant Variables
            services.AddDistributedMemoryCache();
            services.AddSession();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseSession();
            app.UseMvc();
            var httpConfig = new HttpConfiguration();
            httpConfig.MapHttpAttributeRoutes();
        }
    }
}