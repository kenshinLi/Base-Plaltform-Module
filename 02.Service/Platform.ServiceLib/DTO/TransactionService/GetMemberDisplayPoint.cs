using System.Collections.Generic;

namespace PlatformSystem.ServiceLib.Model.TransactionService
{
    public class GetMemberDisplayPoint
    {
        public List<DAOLib.Model.Member> MemberList { get; set; }

        public GetMemberDisplayPoint()
        {
            this.MemberList = new List<DAOLib.Model.Member>();
        }
    }

    public class GetMemberDisplayPointResult
    {
        public int MemberID { get; set; }

        public string AccountName { get; set; }

        public int PointType { get; set; }

        public long Point { get; set; }
    }
}
