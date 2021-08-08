using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamePlatform.DataModel.Model.DB
{
    public class Member
    {
        public int ID { get; set; }

        public int MemberID { get; set; }

        public int AgentID { get; set; }

        public string Subagent { get; set; }

        public int ClusterID { get; set; }

        public int TypeID { get; set; }

        public string UID { get; set; }

        public string LoginCode { get; set; }

        public string AccountName { get; set; }

        public string NickName { get; set; }

        public string APW { get; set; }

        public string FBUID { get; set; }

        public string FBAccount { get; set; }

        public string MobileNumber { get; set; }

        public bool IsAccountBinded { get; set; }

        public bool IsFBBinded { get; set; }

        public bool IsMobileBinded { get; set; }

        public bool IsNickNameBinded { get; set; }

        public int? Gender { get; set; }

        public string Email { get; set; }

        public string Avatar { get; set; }

        public string Description { get; set; }

        public DateTime? Birthday { get; set; }

        public string IMParticipantCode { get; set; }

        public int VIPLevel { get; set; }

        public DateTime? VIPExpirationDateTime { get; set; }

        public long BExp { get; set; }

        public long DExp { get; set; }

        public int Trustworthiness { get; set; }        

        public string Setting { get; set; }

        public string UISetting { get; set; }

        public bool IsEnable { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime? BuildDateTime { get; set; }

        public DateTime? ModifyDateTime { get; set; }

        public DateTime? DisableDateTime { get; set; }

        public DateTime? DeleteDateTime { get; set; }
    }
}
