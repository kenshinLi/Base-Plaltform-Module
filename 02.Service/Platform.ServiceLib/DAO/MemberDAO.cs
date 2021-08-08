using CommonLib.Define;
using CommonLib.Service;
using GamePlatform.DataModel.Model.DB;
using GamePlatform.DataModelLib.Define;
using GamePlatform.DataModelLib.Model.Agent;
using GamePlatform.DataModelLib.Model.Member;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamePlatform.ServiceLib.DAO
{
    public class MemberDAO
    {
        /// <summary>
        /// singleton
        /// </summary>
        private static MemberDAO singleton;
        private ConnectionConfig connConfig;

        /// <summary>
        /// Gets Instance
        /// </summary>
        public static MemberDAO Instance
        {
            get
            {
                if (singleton == null)
                {
                    singleton = new MemberDAO();
                }

                return singleton;
            }
        }

        public MemberDAO()
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
        /// GetMember
        /// </summary>
        /// <param name="GetMemberInfoContent"></param>
        /// <returns></returns>
        public Member GetMember(GetMemberContent content)
        {
            using (var sqlSugar = new SqlSugarClient(connConfig))
            {
                var query = sqlSugar.Queryable<Member>()
                                    .Where(x => x.IsDeleted == false)
                                    .Where(x => x.IsEnable == true);

                if (string.IsNullOrEmpty(content.AccountName) == false)
                    query = query.Where(x => x.AccountName == content.AccountName);
                else if (string.IsNullOrEmpty(content.NickName) == false)
                    query = query.Where(x => x.NickName == content.NickName);
                else if (string.IsNullOrEmpty(content.UID) == false)
                    query = query.Where(x => x.UID == content.UID);
                else if (string.IsNullOrEmpty(content.FBUID) == false)
                    query = query.Where(x => x.FBUID == content.FBUID);
                else if (content.MemberID > 0)
                    query = query.Where(x => x.MemberID == content.MemberID);

                return query.First();
            }
        }

        /// <summary>
        /// GetAgetntMember
        /// </summary>
        /// <param name="agentID"></param>
        /// <param name="accountName"></param>
        /// <returns></returns>
        public Member GetAgetntMember(int agentID, string accountName)
        {
            using (var sqlSugar = new SqlSugarClient(connConfig))
            {
                return sqlSugar.Queryable<Member>()
                                    .Where(x => x.IsDeleted == false)
                                    .Where(x => x.IsEnable == true)
                                    .Where(x => x.AgentID == agentID)
                                    .Where(x => x.AccountName == accountName)
                                    .First();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="accountName"></param>
        /// <param name="nickName"></param>
        /// <param name="pointType"></param>
        /// <returns></returns>
        public Member CreateMember(Agent agent, CheckOrCreateAccountContent content)
        {
            using (var sqlSugar = new SqlSugarClient(connConfig))
            {
                var memberID = sqlSugar.Queryable<Member>().Max<int>("MemberID") + 1;
                var time = DateTime.UtcNow;
                var data = new Member()
                {
                    MemberID = memberID,
                    AgentID = agent.AgentID,
                    ClusterID = 1,
                    TypeID = (int)DataModelLib.Define.MemberType.AGENT,
                    UID = time.Ticks.ToString(),
                    AccountName = content.AccountName,
                    NickName = content.NickName,
                    Subagent = content.Subagent,
                    APW = string.Empty,
                    IsEnable = true,
                    BuildDateTime = time,
                };

                // Insertable Member
                var result = sqlSugar.Insertable<Member>(data)
                    .With(SqlWith.HoldLock)
                    .With(SqlWith.UpdLock)
                    .ExecuteCommand();
                if (result != 1)
                    return null;

                // Insertable MemberCredit
                var credit = new MemberCredit
                {
                    MemberID = memberID,
                    PointType = agent.PointType,
                    Point = 0
                };

                result = sqlSugar.Insertable<MemberCredit>(credit)
                .With(SqlWith.HoldLock)
                .With(SqlWith.UpdLock)
                .ExecuteCommand();
                if (result == 1)
                    return data;
                else
                    return null;
            }
        }

        /// <summary>
        /// UpdateMember
        /// </summary>
        /// <returns></returns>
        public bool UpdateMember(Member member)
        {
            using (var sqlSugar = new SqlSugarClient(connConfig))
            {
                var result = sqlSugar.Updateable(member)
                .With(SqlWith.HoldLock)
                .With(SqlWith.UpdLock)
                .ExecuteCommand();

                return result == 1;
            }
        }


        /// <summary>
        /// GetVipTypeList
        /// </summary>
        /// <param name="memberID"></param>
        /// <returns></returns>
        public List<VIPType> GetVipTypeList()
        {
            using (var sqlSugar = new SqlSugarClient(connConfig))
            {
                return sqlSugar.Queryable<VIPType>().ToList();
            }
        }

        /// <summary>
        /// GetSecondPassword
        /// </summary>
        /// <returns></returns>
        public SecondPW GetSecondPassword(int memberID)
        {
            using (var sqlSugar = new SqlSugarClient(connConfig))
            {
                // return Top 1
                return sqlSugar.Queryable<SecondPW>()
                                    .Where(x => x.MemberID == memberID)
                                    .OrderBy(x => x.BuildDateTime, OrderByType.Desc)//desc
                                    .First();
            }
        }

        /// <summary>
        /// GetTransferStatus
        /// </summary>
        /// <param name="memberID"></param>
        /// <returns></returns>
        public DailyTransferStatus GetTransferStatus(int memberID)
        {
            using (var sqlSugar = new SqlSugarClient(connConfig))
            {
                return sqlSugar.Queryable<DailyTransferStatus>()
                    .Where(x => x.MemberID == memberID)
                    .OrderBy(x => x.BuildDateTime, OrderByType.Desc)//desc
                    .First();
            }
        }

        /// <summary>
        /// CreateTransferStatus
        /// </summary>
        /// <param name="memberID"></param>
        /// <returns></returns>
        public MessageCode CreateTransferStatus(DailyTransferStatus status)
        {
            using (var sqlSugar = new SqlSugarClient(connConfig))
            {
                var result = sqlSugar.Insertable<DailyTransferStatus>(status)
                            .With(SqlWith.HoldLock)
                            .With(SqlWith.UpdLock)
                            .ExecuteCommand();
                if (result != 1)
                    return MessageCode.UNEXPECTED_ERROR;

                return MessageCode.SUCCESS;
            }
        }

        /// <summary>
        /// GetRichestMemberList
        /// </summary>
        /// <param name="Count"></param>
        /// <returns></returns>
        public List<object> GetRichestMemberList()
        {
            using (var sqlSugar = new SqlSugarClient(connConfig))
            {
                var result = sqlSugar.Queryable<Member, MemberCredit>(
                                    (member, credit) => member.MemberID == credit.MemberID)
                                    .OrderBy((member, credit) => credit.Point, OrderByType.Desc)//desc
                                    .Select((member, credit) => new
                                    {
                                        ObjectUID = member.UID,
                                        ObjectPoint = credit.Point
                                    })
                                    .Take(100)
                                    .ToList();

                if (result.Count > 0)
                    return result.ToList<object>();

                return new List<object>();
            }
        }

        /// <summary>
        /// GetMemberCredit
        /// </summary>
        /// <returns></returns>
        public MemberCredit GetMemberCredit(int memberID)
        {
            using (var sqlSugar = new SqlSugarClient(connConfig))
            {
                // Queryable MemberCredit
                var queryResult = sqlSugar.Queryable<MemberCredit>()
                                    .Where(x => x.MemberID == memberID)
                                    .ToList();
                if (queryResult.Count == 1)
                    return queryResult.First();
            }

            return null;
        }

        public long GetMemberDepositPoint(int memberID, DateTime startDateTime, DateTime endDateTime)
        {
            using (var sqlSugar = new SqlSugarClient(connConfig))
            {
                return sqlSugar.Queryable<MemberTransaction>()
                    .Where(x => x.MemberID == memberID)
                    .Where(x => x.Type == (int)TransactionType.DEPOSIT)
                    .Where(x => SqlFunc.Between(x.BuildDateTime, startDateTime, endDateTime))
                    .Sum(x => x.Point);
            }
        }
    }
}
