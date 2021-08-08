using System;
using System.Reflection;
using System.ServiceProcess;

namespace Platform.Application
{
    static class Program
    {
        /// <summary>
        /// 應用程式的主要進入點。
        /// </summary>
        static void Main()
        {
            ServiceBase[] servicesToRun;
            servicesToRun = new ServiceBase[]
            {
                new WinService()
            };

            if (Environment.UserInteractive)
            {
                //console偵錯模式 02 - 使用RunInteractive來執行原有Service功能以進行偵錯
                Program.RunInteractive(servicesToRun);
            }
            else
            {
                // window service模式
                ServiceBase.Run(servicesToRun);
            }
        }

        static void RunInteractive(ServiceBase[] servicesToRun)
        {
            // 利用Reflection取得非公開之 OnStart() 方法資訊
            MethodInfo onStartMethod = typeof(ServiceBase).GetMethod("OnStart", BindingFlags.Instance | BindingFlags.NonPublic);

            // 執行 OnStart 方法
            foreach (ServiceBase service in servicesToRun)
            {
                Console.WriteLine("Starting {0}...", service.ServiceName);
                onStartMethod.Invoke(service, new object[] { new string[] { } });
                Console.WriteLine("Started");
            }

            Console.WriteLine("Press any key to stop the services");
            Console.ReadKey();

            // 利用Reflection取得非公開之 OnStop() 方法資訊
            MethodInfo onStopMethod = typeof(ServiceBase).GetMethod("OnStop", BindingFlags.Instance | BindingFlags.NonPublic);

            // 執行 OnStop 方法
            foreach (ServiceBase service in servicesToRun)
            {
                Console.Write("Stopping {0}...", service.ServiceName);
                onStopMethod.Invoke(service, null);
                Console.WriteLine("Stopped");
            }
        }
    }

    
}
