using CommonLib.Define;
using CommonLib.Service;
using GamePlatform.DataModel.Model.DB;
using GamePlatform.DataModelLib.Define;
using GamePlatform.DataModelLib.Model.Agent;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamePlatform.ServiceLib.DAO
{
    public class AgentDAO
    {
        /// <summary>
        /// singleton
        /// </summary>
        private static AgentDAO singleton;
        private ConnectionConfig connConfig;

        /// <summary>
        /// Gets Instance
        /// </summary>
        public static AgentDAO Instance
        {
            get
            {
                if (singleton == null)
                {
                    singleton = new AgentDAO();
                }

                return singleton;
            }
        }

        public AgentDAO()
        {
            var connections = AppSettingService.Instace.ConnectionStrings;
            var key = GamePlatformConnectionType.GAME_PLATFORM.ToString();
            if (connections.ContainsKey(key) == false)
                throw new Exception(string.Format("key is null: {0}", key));

            var connectionString = connections[key].ConnectionString;
            if (string.IsNullOrEmpty(connectionString))
                throw new Exception(string.Format("ConnectionString is null: {0}", key));

            connConfig = new ConnectionConfig()
            {
                ConnectionString = connectionString,
                DbType = DbType.SqlServer,
                IsAutoCloseConnection = true,
                InitKeyType = InitKeyType.SystemTable,
            };
        }

        public Agent GetAgent(GetAgentContent content)
        {
            using (var sqlSugar = new SqlSugarClient(connConfig))
            {
                var queryResult = sqlSugar.Queryable<Agent>()
                                    .WhereIF(string.IsNullOrEmpty(content.AgentCode) == false, x => x.AgentCode == content.AgentCode)
                                    .WhereIF(content.AgentID > 0, x => x.AgentID == content.AgentID)
                                    .Single();

                return queryResult;
            }
        }

        public MessageCode SaveAgentMemberLoginVerification(AgentMemberLoginVerification content)
        {
            using (var sqlSugar = new SqlSugarClient(connConfig))
            {
                // Insertable AgentMemberLoginVerification
                var result = sqlSugar.Insertable<AgentMemberLoginVerification>(content)
                            .With(SqlWith.HoldLock)
                            .With(SqlWith.UpdLock)
                            .ExecuteCommand();

                if (result != 1)
                    return MessageCode.UNEXPECTED_ERROR;

                return MessageCode.SUCCESS;
            }
        }

        public List<AllowGameGroup> GetAllowGameGroup(int agentID)
        {
            using (var sqlSugar = new SqlSugarClient(connConfig))
            {
                return sqlSugar.Queryable<AllowGameGroup>()
                                    .Where(x => x.AgentID == agentID)
                                    .ToList();
            }
        }

        /// <summary>
        /// GetWagers
        /// </summary>
        /// <returns></returns>
        public MessageCode GetWagers(GetWagersContent content, Agent agent, out object result)
        {
            result = null;
            var list = new List<GetWagersResult>();
            var queryList = new List<Wager>();
            var totalNumber = 0;

            //GET QueryMode
            if (Enum.TryParse(content.QueryMode.ToString(), out GetWagersQueryMode queryMode) == false)
                return MessageCode.ILLEGAL_INPUT;

            using (var sqlSugar = new SqlSugarClient(connConfig))
            {
                var queryCondition = sqlSugar.Queryable<Wager>()
                                    .Where(x => x.AgentID == agent.AgentID)
                                    .Where(x => x.MemberType == (int)DataModelLib.Define.MemberType.AGENT)
                                    .OrderBy(x => x.ID, OrderByType.Asc);

                if(content.QueryMode == (int)GetWagersQueryMode.BATCH)
                {
                    if (content.Count <= 0 ||
                        content.Count > 100)
                    {
                        return MessageCode.ILLEGAL_INPUT;
                    }

                    Wager find = null;
                    if (string.IsNullOrEmpty(content.AboveSerial))
                    {
                        queryList = sqlSugar.Queryable<Wager>()
                                       .Where(x => x.AgentID == agent.AgentID)
                                       .Where(x => x.MemberType == (int)DataModelLib.Define.MemberType.AGENT)
                                       .Where(x => SqlFunc.DateIsSame(x.WagerDateTime, DateTime.UtcNow))
                                       .OrderBy(x => x.ID, OrderByType.Asc)
                                       .Take(content.Count)
                                       .ToList();
                    }
                    else
                    {
                        find = sqlSugar.Queryable<Wager>()
                                       .Where(x => x.AgentID == agent.AgentID)
                                       .Where(x => x.Serial == content.AboveSerial)
                                       .Single();

                        if (find == null)
                            return MessageCode.ILLEGAL_INPUT;

                        queryList = queryCondition.Where(x => x.ID > find.ID)
                                        .Take(content.Count)
                                        .ToList();
                    }

                    totalNumber = queryList.Count;
                }
                else if (content.QueryMode == (int)GetWagersQueryMode.TIME_INTERVAL)
                {
                    if (content.StartDateTime == null ||
                        content.EndDateTime == null ||
                        content.RowsPerPage <= 0 ||
                        content.RowsPerPage > 100 ||
                        content.PageNo <= 0)
                    {
                        return MessageCode.ILLEGAL_INPUT;
                    }

                    queryList = queryCondition.Where(x => SqlFunc.Between(x.WagerDateTime, content.StartDateTime, content.EndDateTime))
                                              .ToPageList(content.PageNo, content.RowsPerPage, ref totalNumber);
                }

                foreach (var item in queryList)
                {
                    list.Add(new GetWagersResult
                    {
                        Serial = item.Serial,
                        AgentCode = agent.AgentCode,
                        AccountName = item.AccountName,
                        Subagent = item.Subagent,
                        GameID = item.GameID,
                        ProfitMode = item.ProfitMode,
                        BetPoint = item.BetPoint,
                        WinPoint = item.WinPoint,
                        BeforePoint = item.BeforePoint,
                        AfterPoint = item.AfterPoint,
                        Fee = item.Fee,
                        WagerDateTime = item.WagerDateTime
                    });
                }

                result = new
                {
                    TotalRowsCount = totalNumber,
                    List = list
                };

                return MessageCode.SUCCESS;
            }
        }
    }
}
