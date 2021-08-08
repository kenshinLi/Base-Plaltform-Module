using CommonLib.Extension;
using CommonLib.Model;
using CommonLib.Utility;
using Platform.DAOLib.Model.DB;

namespace Platform.DAOLib.DAO
{
    public class AgentDAO : BaseDAO
    {
        internal AgentDAO(DbConnectInfo dbConnectInfo) : base(dbConnectInfo)
        {

        }

        public Agent GetAgent(int agentID, string agentCode = "")
        {
            using (var sqlSugar = base.GetInstance(true))
            {
                return sqlSugar.Queryable<Agent>()
                    .WhereIF(string.IsNullOrEmpty(agentCode) == false, x => x.AgentCode == agentCode)
                    .WhereIF(agentID > 0, x => x.AgentID == agentID)                    
                    .OverwriteParameter(x => x.AgentCode, System.Data.DbType.AnsiString, 50)
                    .Single();               
            }
        }
    }
}
