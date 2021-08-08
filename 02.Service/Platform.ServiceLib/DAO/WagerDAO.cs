using CommonLib.Define;
using CommonLib.Service;
using GamePlatform.DataModel.Model.DB;
using GamePlatform.DataModelLib.Define;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamePlatform.ServiceLib.DAO
{
    public class WagerDAO
    {
        /// <summary>
        /// singleton
        /// </summary>
        private static WagerDAO singleton;
        private ConnectionConfig connConfig;

        /// <summary>
        /// Gets Instance
        /// </summary>
        public static WagerDAO Instance
        {
            get
            {
                if (singleton == null)
                {
                    singleton = new WagerDAO();
                }

                return singleton;
            }
        }

        public WagerDAO()
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

        /// <summary>
        /// ImportWagers
        /// </summary>
        /// <returns></returns>
        public MessageCode ImportWagers(List<Wager> list)
        {
            using (var sqlSugar = new SqlSugarClient(connConfig))
            {
                // Insertable Wager
                var result = sqlSugar.Insertable<Wager>(list)
                            //.With(SqlWith.HoldLock)
                            //.With(SqlWith.UpdLock)
                            .ExecuteCommand();

                if (result == list.Count)
                    return MessageCode.SUCCESS;
                else
                    return MessageCode.DENY_ACCESS;
            }
        }

        /// <summary>
        /// QueryLastWager
        /// </summary>
        /// <returns></returns>
        public Wager QueryLastWager(int memberID, string gameTicket)
        {
            using (var sqlSugar = new SqlSugarClient(connConfig))
            {
                // Queryable Wager
                return sqlSugar.Queryable<Wager>()
                        .Where(x => x.MemberID == memberID)
                        .Where(x => x.GameTicket == gameTicket)
                        .OrderBy(x => x.WagerDateTime, OrderByType.Desc)
                        .First();
            }
        }

        /// <summary>
        /// BackupWagers
        /// </summary>
        /// <returns></returns>
        public int BackupWagers(DateTime StartDateTime, DateTime EndDateTime)
        {
            EndDateTime = EndDateTime.AddMilliseconds(-1);

            using (var sqlSugar = new SqlSugarClient(connConfig))
            {
                #region SQL COMMAND
                var sql = @"INSERT INTO [WagerHistory]
                                        ([Serial]
                                        ,[GameTicket]
                                        ,[MemberOnlineToken]
                                        ,[MemberID]
                                        ,[MemberType]
                                        ,[AgentID]
                                        ,[AgentCode]
                                        ,[Subagent]
                                        ,[AccountName]
                                        ,[GameID]
                                        ,[GroupID]
                                        ,[TableID]
                                        ,[PointType]
                                        ,[ProfitMode]
                                        ,[BetPoint]
                                        ,[WinPoint]
                                        ,[BeforePoint]
                                        ,[AfterPoint]
                                        ,[Fee]
                                        ,[Detail]
                                        ,[WagerDateTime])
                        SELECT 
                                        [Serial]
                                        ,[GameTicket]
                                        ,[MemberOnlineToken]
                                        ,[MemberID]
                                        ,[MemberType]
                                        ,[AgentID]
                                        ,[AgentCode]
                                        ,[Subagent]
                                        ,[AccountName]
                                        ,[GameID]
                                        ,[GroupID]
                                        ,[TableID]
                                        ,[PointType]
                                        ,[ProfitMode]
                                        ,[BetPoint]
                                        ,[WinPoint]
                                        ,[BeforePoint]
                                        ,[AfterPoint]
                                        ,[Fee]
                                        ,[Detail]
                                        ,[WagerDateTime]
                        FROM [Wager] 
                        WHERE [WagerDateTime] BETWEEN @StartDateTime AND @EndDateTime";
                #endregion

                return sqlSugar.Ado.ExecuteCommand(sql,
                    new List<SugarParameter>(){
                        new SugarParameter("@StartDateTime", StartDateTime),
                        new SugarParameter("@EndDateTime", EndDateTime)
                    });
            }
        }
    }
}
