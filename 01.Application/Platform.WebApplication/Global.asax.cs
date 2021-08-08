
using CommonLib.Utility;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;

namespace Platform.WebApplication
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);

            var services = new ServiceCollection();
            ConfigureServices(services);
            //Program.Main(new string[] { });
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpClient<HttpClientUtility>();
        }
    }
}
