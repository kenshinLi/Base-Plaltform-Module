using CommonLib.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Platform.ServiceLib.DTO.RequestBody
{
    public class AgentWalletServiceRequestBody : BaseRequestBody
    {
        public string AgentWalletKey { get; set; }

        public string AgentWalletToken { get; set; }
    }
}
