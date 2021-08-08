using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Platform.DAOLib.Model.DB
{
    public class AgentAuthToken
    {
        public int ID { get; set; }        

        public int AgentID { get; set; }

        public string Token { get; set; }
    }
}
