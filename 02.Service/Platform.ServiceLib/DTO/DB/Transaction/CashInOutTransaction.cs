using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamePlatform.DataModel.Model.DB
{
    public class CashInOutTransaction
    {
        public int ID { get; set; }

        public string Serial { get; set; }

        public int Status { get; set; }
        
        public string AccessCode { get; set; }        

        public int MemberID { get; set; }

        public int AgentID { get; set; }

        public int Direction { get; set; }

        public int PointType { get; set; }

        public long? BeforePoint { get; set; }

        public long Point { get; set; }

        public long? AfterPoint { get; set; }

        public bool IsCashOutAll { get; set; }        

        public DateTime BuildDateTime { get; set; }

        public DateTime? TranscationDateTime { get; set; }
    }
}
