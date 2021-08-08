using CommonLib.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlatformSystem.ServiceLib.Model.Client
{
    public class GetWagerPageContent : BaseScrollingListContent
    {
        public int GameID { get; set; }

        public List<int> GroupList { get; set; }
    }
}
