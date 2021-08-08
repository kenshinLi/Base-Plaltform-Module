using System;
using System.Collections.Generic;
using System.Text;

namespace GamePlatform.DataModel.Model.DB
{
    public class Wager
    {
        public int ID { get; set; }

        public string Serial { get; set; }

        public string GameTicket { get; set; }        

        public string MemberOnlineToken { get; set; }

        public int MemberID { get; set; }

        public int AgentID { get; set; }

        public int MemberType { get; set; }

        public string Subagent { get; set; }

        public string AgentCode { get; set; }

        public string AccountName { get; set; }

        public int GameID { get; set; }

        public int GroupID { get; set; }

        public int TableID { get; set; }

        public int PointType { get; set; }

        public int ProfitMode { get; set; }

        public long BetPoint { get; set; }

        public long WinPoint { get; set; }

        public long Fee { get; set; }

        public long BeforePoint { get; set; }

        public long AfterPoint { get; set; }

        public string Detail { get; set; }

        public DateTime WagerDateTime { get; set; }
    }
}
