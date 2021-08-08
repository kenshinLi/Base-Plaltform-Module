using PlatformSystem.ServiceLib.Model.Agent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlatformSystem.ServiceLib.Model.Setting
{
    public class AgentTestSetting
    {
        public string AgentCode { get; set; }

        public int ProcessIntervalTime { get; set; }

        public int ProcessCount { get; set; }

        public int QueryIntervalMinute { get; set; }

        public bool LocalMode { get; set; }

        public GetWagersContent GetWagersContent { get; set; }

        public AgentTestSetting()
        {
            ProcessIntervalTime = 60;
            QueryIntervalMinute = 5;

            GetWagersContent = new GetWagersContent();
        }
    }
}
