using CommonLib.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace Platform.WebApplication
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
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
        }
    }
}
