using CommonLib.Service;
using Platform.ServiceLib.Factory;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CommonLib.Process;

namespace Platform.ServiceLib.Process
{
    public class ScheduleProcess : BaseProcess
    {
        #region Property

        private CustomSetting customSetting;       

        #endregion Property

        #region Method

        public ScheduleProcess()
        {
            if (AppSettingService.Instace.CustomSetting != null)
                customSetting = JsonConvert.DeserializeObject<CustomSetting>(AppSettingService.Instace.CustomSetting.ToString());
        }

        protected override void ProcessMethod()
        {
            //注單處理工作
            //ServiceFactory.Schedule.FactoryProcess();         
        }
             
        #endregion
    }

    public class CustomSetting
    {

    }
}
