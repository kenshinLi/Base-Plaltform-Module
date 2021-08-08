using CommonLib.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlatformSystem.ServiceLib.Model.TransactionService
{
    public class UpdateGamePointContent
    {
        public string AccessSerial { get; set; }

        public string GameTicket { get; set; }

        public string MemberOnlineToken { get; set; }

        public int MemberID { get; set; }

        public long Point { get; set; }

        public int Direction { get; set; }

        public int PointType { get; set; }

        public bool IsAllin { get; set; }
    }
}
