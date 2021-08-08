using PlatformSystem.DataModel.Model.WagerService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlatformSystem.ServiceLib.Model.Client
{
    public class GetWagerPageResult
    {
        public int TotalCount { get; set; }

        public List<WagerResult> List { get; set; }
    }
}
