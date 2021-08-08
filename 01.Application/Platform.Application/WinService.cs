
using System;
using System.Configuration;
using System.Diagnostics;
using System.ServiceProcess;
using CommonLib.Service;
using Microsoft.Owin.Hosting;
using Newtonsoft.Json;

namespace Platform.Application
{
    public partial class WinService : ServiceBase
    {
        private IDisposable myServer;
        public WinService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {           
            // Start OWIN host
            if (string.IsNullOrEmpty(AppSettingService.Instace.WebApiURL))
                throw new Exception("WebApiURL NOT SETTING");

            myServer = WebApp.Start(AppSettingService.Instace.WebApiURL);

            Console.WriteLine("Stoping Platform.Application");
        }

        protected override void OnStop()
        {
            if (string.IsNullOrEmpty(AppSettingService.Instace.WebApiURL))
                myServer.Dispose();

            Console.WriteLine("Stoping Platform.Application");
        }
    }
}
