using CommonLib.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using Owin;
using Microsoft.Owin;
using System.Web.Http.SelfHost;
using Microsoft.Extensions.DependencyInjection;
using CommonLib.Utility;

[assembly: OwinStartup(typeof(Platform.Application.Startup))]
namespace Platform.Application
{    
    public class Startup
    {
        // This code configures Web API. The Startup class is specified as a type
        // parameter in the WebApp.Start method.
        public void Configuration(IAppBuilder appBuilder)
        {
            // Configure Web API for self-host. 

            var config = new HttpConfiguration();
            // Web API 設定和服務
            config.EnableCors();

            config.MapHttpAttributeRoutes();
            config.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always;

            foreach (var controllerName in AppSettingService.Instace.ControllerNameList)
            {
                // Web API 路由
                var route = string.Format("api/{0}/", controllerName);

                config.Routes.MapHttpRoute(
                    name: controllerName,
                    routeTemplate: route + "{action}",
                    defaults: new
                    {
                        controller = controllerName
                    }
                );
            }

            appBuilder.UseWebApi(config);

            var services = new ServiceCollection();
            ConfigureServices(services);
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpClient<HttpClientUtility>();
        }
    }
}
