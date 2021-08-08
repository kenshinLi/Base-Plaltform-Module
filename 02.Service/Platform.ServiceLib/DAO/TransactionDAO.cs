using CommonLib.Define;
using CommonLib.Service;
using GamePlatform.DataModel.Model.DB;
using GamePlatform.DataModelLib.Define;
using GamePlatform.DataModelLib.Model.Agent;
using GamePlatform.DataModelLib.Model.TransactionService;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamePlatform.ServiceLib.DAO
{
    public class TransactionDAO
    {
        /// <summary>
        /// singleton
        /// </summary>
        private static TransactionDAO singleton;
        private ConnectionConfig connConfig;

        /// <summary>
        /// Gets Instance
        /// </summary>
        public static TransactionDAO Instance
        {
            get
            {
                if (singleton == null)
                {
                    singleton = new TransactionDAO();
                }

                return singleton;
            }
        }

        public TransactionDAO()
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
        /// InitiateCashInOut
        /// </summary>
        /// <returns></returns>
        public MessageCode InitiateCashInOut(InitiateCashInOutContent content, MemberCredit credit, out CashInOutTransaction cashInOutTrans)
        {
            using (var sqlSugar = new SqlSugarClient(connConfig))
            {
                cashInOutTrans = sqlSugar.Queryable<CashInOutTransaction>()
                   .Where(x => x.AccessCode == content.AccessCode)
                   .Single();
                if (cashInOutTrans != null)
                    return MessageCode.DENY_ACCESS;

                Enum.TryParse(content.Direction.ToString(), out CashInOutType direction);

                var serial = Guid.NewGuid().ToString();
                var time = DateTime.UtcNow;

                if (direction == CashInOutType.OUT)
                    content.Point = (-content.Point);

                // Insertable MemberTransaction
                var trans = new MemberTransaction
                {
                    MemberID = content.MemberID,
                    UID = content.MemberUID,
                    ObjectID = 1,
                    Type = (int)TransactionType.CASH_IN_OUT,
                    Mapping = serial,
                    PointType = credit.PointType,
                    Point = content.Point,
                    ItemContent = string.Empty,
                    State = (int)TransactionState.READY,                    
                    BuildDateTime = time
                };

                var result = sqlSugar.Insertable<MemberTransaction>(trans)
                    .With(SqlWith.HoldLock)
                    .With(SqlWith.UpdLock)
                    .ExecuteCommand();
                if (result != 1)
                    return MessageCode.UNEXPECTED_ERROR;

                // Insertable CashInOutTransaction                
                cashInOutTrans = new CashInOutTransaction
                {
                    Serial = serial,
                    Status = (int)CashInOutStatus.READY,
                    AccessCode = content.AccessCode,
                    MemberID = content.MemberID,
                    AgentID = content.AgentID,
                    Direction = (int)direction,
                    PointType = credit.PointType,
                    Point = Math.Abs(content.Point),
                    IsCashOutAll = content.IsCashOutAll,
                    BuildDateTime = time,
                };

                result = sqlSugar.Insertable<CashInOutTransaction>(cashInOutTrans)
                    .With(SqlWith.HoldLock)
                    .With(SqlWith.UpdLock)
                    .ExecuteCommand();
                if (result != 1)
                    return MessageCode.UNEXPECTED_ERROR;

                return MessageCode.SUCCESS;
            }
        }

        /// <summary>
        /// QueryCashInOut
        /// </summary>
        /// <returns></returns>
        public MessageCode QueryCashInOut(CashInOutContent content, out CashInOutTransaction transaction)
        {
            using (var sqlSugar = new SqlSugarClient(connConfig))
            {
                transaction = sqlSugar.Queryable<CashInOutTransaction>()
                   .Where(x => x.Serial == content.TransactionSerial)
                   .Where(x => x.AccessCode == content.AccessCode)
                   .Single();
                if (transaction == null)
                    return MessageCode.ILLEGAL_INPUT;
                else if (transaction.Status != (int)CashInOutStatus.READY)
                    return MessageCode.HAS_FINISHED;

                return MessageCode.SUCCESS;
            }
        }

        /// <summary>
        /// CashInOut
        /// </summary>
        /// <returns></returns>
        public MessageCode CashInOut(ref CashInOutTransaction transaction, MemberCredit credit)
        {
            using (var sqlSugar = new SqlSugarClient(connConfig))
            {
                if (transaction.IsCashOutAll)
                    transaction.Point = credit.Point;

                // Updateable MemberCredit
                var beforePoint = credit.Point;
                var point = transaction.Direction == (int)CashInOutType.OUT ? -(transaction.Point) : transaction.Point;                

                credit.Point += point;

                var result = sqlSugar.Updateable(credit)
                    .With(SqlWith.HoldLock)
                    .With(SqlWith.UpdLock)
                    .ExecuteCommand();
                if (result != 1)
                    return MessageCode.UNEXPECTED_ERROR;

                // Insertable PointUpdateHistory
                var time = DateTime.UtcNow;
                var history = new PointUpdateHistory
                {
                    MemberID = transaction.MemberID,
                    TypeID = (int)TransactionType.CASH_IN_OUT,
                    PointType = credit.PointType,
                    BeforePoint = beforePoint,
                    Point = point,
                    AfterPoint = credit.Point,
                    Comment = transaction.Serial,
                    BuildDateTime = time,
                };

                result = sqlSugar.Insertable<PointUpdateHistory>(history)
                    .With(SqlWith.HoldLock)
                    .With(SqlWith.UpdLock)
                    .ExecuteCommand();
                if (result != 1)
                    return MessageCode.UNEXPECTED_ERROR;

                // Queryable and Updateable MemberTransaction
                var memberID = transaction.MemberID;
                var serial = transaction.Serial;

                var memberTrans = sqlSugar.Queryable<MemberTransaction>()
                                   .Where(x => x.MemberID == memberID)
                                   .Where(x => x.Mapping == serial)
                                   .Single();

                memberTrans.State = (int)TransactionState.SUCCESS;
                memberTrans.FinishedDateTime = time;

                if (transaction.IsCashOutAll)
                    memberTrans.Point = point;

                result = sqlSugar.Updateable<MemberTransaction>(memberTrans)
                    .With(SqlWith.HoldLock)
                    .With(SqlWith.UpdLock)
                    .ExecuteCommand();
                if (result != 1)
                    return MessageCode.UNEXPECTED_ERROR;

                // Updateable CashInOutTransaction
                transaction.BeforePoint = beforePoint;
                transaction.AfterPoint = credit.Point;
                transaction.Status = (int)CashInOutStatus.SUCCESS;
                transaction.TranscationDateTime = time;

                result = sqlSugar.Updateable<CashInOutTransaction>(transaction)
                    .With(SqlWith.HoldLock)
                    .With(SqlWith.UpdLock)
                    .ExecuteCommand();
                if (result != 1)
                    return MessageCode.UNEXPECTED_ERROR;

                return MessageCode.SUCCESS;
            }
        }


        /// <summary>
        /// DepositPoint
        /// </summary>
        /// <returns></returns>
        public DepositTransaction DepositPoint(DepositPointContent content)
        {
            var serial = Guid.NewGuid().ToString();

            using (var sqlSugar = new SqlSugarClient(connConfig))
            {
                var depositTrans = sqlSugar.Queryable<DepositTransaction>()
                                       .Where(x => x.AccessKey == content.AccessKey)
                                       .Single();
                if (depositTrans != null)
                    return depositTrans;

                // Updateable MemberCredit
                var credit = MemberDAO.Instance.GetMemberCredit(content.MemberID);
                if (credit == null)
                    return null;

                var beforePoint = credit.Point;
                credit.Point += content.Point;

                var result = sqlSugar.Updateable(credit)
                    .With(SqlWith.HoldLock)
                    .With(SqlWith.UpdLock)
                    .ExecuteCommand();
                if (result != 1)
                    return null;

                // Insertable PointUpdateHistory
                var time = DateTime.UtcNow;
                var history = new PointUpdateHistory
                {
                    MemberID = content.MemberID,
                    TypeID = (int)TransactionType.DEPOSIT,
                    PointType = content.PointType,
                    BeforePoint = beforePoint,
                    Point = content.Point,
                    AfterPoint = credit.Point,
                    Comment = serial,
                    BuildDateTime = time,
                };

                result = sqlSugar.Insertable<PointUpdateHistory>(history)
                    .With(SqlWith.HoldLock)
                    .With(SqlWith.UpdLock)
                    .ExecuteCommand();
                if (result != 1)
                    return null;

                // Insertable MemberTransaction
                var trans = new MemberTransaction
                {
                    MemberID = content.MemberID,
                    UID = content.MemberUID,
                    ObjectID = 1,
                    Type = (int)TransactionType.DEPOSIT,
                    Mapping = serial,
                    PointType = content.PointType,
                    Point = content.Point,
                    ItemContent = string.Empty,
                    State = (int)TransactionState.SUCCESS,
                    BuildDateTime = time,
                    FinishedDateTime = time
                };

                result = sqlSugar.Insertable<MemberTransaction>(trans)
                    .With(SqlWith.HoldLock)
                    .With(SqlWith.UpdLock)
                    .ExecuteCommand();
                if (result != 1)
                    return null;

                // Insertable DepositTransaction
                depositTrans = new DepositTransaction
                {
                    Serial = serial,
                    AccessKey = content.AccessKey,
                    MemberID = content.MemberID,
                    DepositType = content.DepositType,
                    PointType = content.PointType,
                    BeforePoint = beforePoint,
                    Point = content.Point,
                    AfterPoint = credit.Point,
                    BuildDateTime = time,
                };

                result = sqlSugar.Insertable<DepositTransaction>(depositTrans)
                    .With(SqlWith.HoldLock)
                    .With(SqlWith.UpdLock)
                    .ExecuteCommand();
                if (result != 1)
                    return null;

                return depositTrans;
            }
        }

        /// <summary>
        /// InitiateTransferation
        /// </summary>
        /// <param name="memberID"></param>
        /// <returns></returns>
        public string InitiateTransferation(InitiateTransferationContent content, MemberCredit InitiatorCredit, MemberCredit AcceptorCredit)
        {
            using (var sqlSugar = new SqlSugarClient(connConfig))
            {
                var time = DateTime.UtcNow;
                var serial = Guid.NewGuid().ToString();
                var subjectBeforePoint = InitiatorCredit.Point;

                // Insertable TransferTransaction
                var data = new TransferTransaction
                {
                    Serial = serial,
                    InitiatorID = InitiatorCredit.MemberID,
                    InitiatorUID = content.InitiatorUID,
                    AcceptorID = AcceptorCredit.MemberID,
                    AcceptorUID = content.AcceptorUID,
                    PointType = (int)PointType.USD,
                    Point = content.TransferPoints,
                    Fee = content.TransferFee,
                    State = (int)TransferState.INITIATE,
                    BuildDateTime = time,
                    InitiateDateTime = time
                };

                var result = sqlSugar.Insertable<TransferTransaction>(data)
                    .With(SqlWith.HoldLock)
                    .With(SqlWith.UpdLock)
                    .ExecuteCommand();
                if (result != 1)
                    return string.Empty;

                // Updateable MemberCredit
                InitiatorCredit.Point -= (content.TransferPoints + content.TransferFee);

                result = sqlSugar.Updateable(InitiatorCredit)
                    .With(SqlWith.HoldLock)
                    .With(SqlWith.UpdLock)
                    .ExecuteCommand();
                if (result != 1)
                    return string.Empty;

                // Insertable PointUpdateHistory
                var history = new PointUpdateHistory
                {
                    MemberID = InitiatorCredit.MemberID,
                    TypeID = (int)TransactionType.TRANSFER,
                    PointType = (int)PointType.USD,
                    BeforePoint = subjectBeforePoint,
                    Point = -(content.TransferPoints + content.TransferFee),
                    AfterPoint = InitiatorCredit.Point,
                    Comment = serial,
                    BuildDateTime = time,
                };

                result = sqlSugar.Insertable<PointUpdateHistory>(history)
                    .With(SqlWith.HoldLock)
                    .With(SqlWith.UpdLock)
                    .ExecuteCommand();
                if (result != 1)
                    return string.Empty;

                // Insertable MemberTransaction InitiatorID
                var trans = new MemberTransaction
                {
                    MemberID = InitiatorCredit.MemberID,
                    UID = content.InitiatorUID,
                    ObjectID = AcceptorCredit.MemberID,
                    ObjectUID = content.AcceptorUID,
                    Type = (int)TransactionType.TRANSFER,
                    Mapping = serial,
                    PointType = (int)PointType.USD,
                    Point = -content.TransferPoints,
                    ItemContent = string.Empty,
                    State = (int)TransactionState.WAITING,
                    BuildDateTime = time
                };

                result = sqlSugar.Insertable<MemberTransaction>(trans)
                    .With(SqlWith.HoldLock)
                    .With(SqlWith.UpdLock)
                    .ExecuteCommand();
                if (result != 1)
                    return string.Empty;

                // Insertable MemberTransaction AcceptorID
                trans = new MemberTransaction
                {
                    MemberID = AcceptorCredit.MemberID,
                    UID = content.AcceptorUID,
                    ObjectID = InitiatorCredit.MemberID,
                    ObjectUID = content.InitiatorUID,
                    Type = (int)TransactionType.TRANSFER,
                    Mapping = serial,
                    PointType = (int)PointType.USD,
                    Point = content.TransferPoints,
                    ItemContent = string.Empty,
                    State = (int)TransactionState.READY,
                    BuildDateTime = time
                };

                result = sqlSugar.Insertable<MemberTransaction>(trans)
                    .With(SqlWith.HoldLock)
                    .With(SqlWith.UpdLock)
                    .ExecuteCommand();
                if (result == 1)
                    return serial;
                else
                    return string.Empty;
            }
        }

        /// <summary>
        /// VerifyTransferation
        /// </summary>
        /// <returns></returns>
        public TransferTransaction VerifyTransferation(VerifyTransferationContent content)
        {
            using (var sqlSugar = new SqlSugarClient(connConfig))
            {
                // Queryable TransferTransaction
                var transferData = sqlSugar.Queryable<TransferTransaction>()
                    .Where(x => x.Serial == content.TransactionSerial)
                    .Where(x => x.State == (int)TransferState.CONFIRM)
                    .Where(x => x.InitiatorID == content.InitiatorID)
                    .Single();
                if (transferData == null)
                    return null;

                // 轉帳處理
                var state = content.YesOrNo ? TransferState.COMPLETE : TransferState.CANCEL;

                return Transferation(transferData, state);
            }
        }

        /// <summary>
        /// Transferation
        /// </summary>
        /// <returns></returns>
        public TransferTransaction Transferation(TransferTransaction transferData, TransferState state)
        {
            using (var sqlSugar = new SqlSugarClient(connConfig))
            {
                // Queryable MemberTransaction
                var TransDataList = sqlSugar.Queryable<MemberTransaction>()
                    .Where(x => x.Mapping == transferData.Serial)
                    .Where(x => x.State < (int)TransactionState.SUCCESS)
                    .ToList();
                if (TransDataList == null || TransDataList.Count != 2)
                    return null;

                // 轉帳 或 退款
                int updateTargetID = (state == TransferState.COMPLETE) ? transferData.AcceptorID : transferData.InitiatorID;
                MemberCredit credit = MemberDAO.Instance.GetMemberCredit(updateTargetID);
                long beforePoint = credit.Point;

                if (state != TransferState.COMPLETE)
                    transferData.Point += transferData.Fee;

                credit.Point += transferData.Point;

                // Updateable MemberCredit
                var creditResult = sqlSugar.Updateable(credit)
                    .With(SqlWith.HoldLock)
                    .With(SqlWith.UpdLock)
                    .ExecuteCommand();
                if (creditResult != 1)
                    return null;

                // Insertable PointUpdateHistory
                var time = DateTime.UtcNow;
                var history = new PointUpdateHistory
                {
                    MemberID = updateTargetID,
                    TypeID = (int)TransactionType.TRANSFER,
                    PointType = (int)PointType.USD,
                    BeforePoint = beforePoint,
                    Point = transferData.Point,
                    AfterPoint = credit.Point,
                    Comment = transferData.Serial,
                    BuildDateTime = time,
                };

                var logResult = sqlSugar.Insertable<PointUpdateHistory>(history)
                    .With(SqlWith.HoldLock)
                    .With(SqlWith.UpdLock)
                    .ExecuteCommand();
                if (logResult != 1)
                    return null;

                // Updateable TransferTransaction
                transferData.State = (int)state;
                transferData.FinishedDateTime = time;

                if (state == TransferState.COMPLETE)
                {
                    transferData.VerifyDateTime = time;
                    transferData.TransferDatetime = time;
                }

                var result = sqlSugar.Updateable(transferData)
                .With(SqlWith.HoldLock)
                .With(SqlWith.UpdLock)
                .ExecuteCommand();

                if (result != 1)
                    return null;

                // Updateable MemberTransaction
                foreach (var item in TransDataList)
                {
                    if (state == TransferState.COMPLETE)
                        item.State = (int)TransactionState.SUCCESS;
                    else if (state == TransferState.TIMEOUT)
                    {
                        item.State = (int)TransactionState.TIMEEDOUT;
                        item.TimedOutDateTime = time;
                    }
                    else
                        item.State = (int)TransactionState.FAILED;

                    item.FinishedDateTime = time;
                }

                var transResult = sqlSugar.Updateable(TransDataList)
                    .With(SqlWith.HoldLock)
                    .With(SqlWith.UpdLock)
                    .ExecuteCommand();
                if (transResult != 2)
                    return null;

                return transferData;
            }
        }

        /// <summary>
        /// UpdateGamePoint
        /// </summary>
        /// <param name="memberID"></param>
        /// <returns></returns>
        public MessageCode UpdateGamePoint(UpdateGamePointContent content, out GamePointTransaction trans)
        {
            trans = null;

            // Queryable MemberCredit
            var credit = MemberDAO.Instance.GetMemberCredit(content.MemberID);
            if (credit == null)
                return MessageCode.UNEXPECTED_ERROR;

            // NOT_ENOUGH
            if (content.Direction == (int)GamePointTransactionType.ADVANCE &&
               credit.Point < content.Point)
                return MessageCode.NOT_ENOUGH;

            using (var sqlSugar = new SqlSugarClient(connConfig))
            {
                var accessSerial = content.AccessSerial;

                trans = sqlSugar.Queryable<GamePointTransaction>()
                            .Where(x => x.Serial == accessSerial)
                            .Single();
                if (trans != null)
                    return MessageCode.SUCCESS;

                // Updateable MemberCredit
                var beforePoint = credit.Point;
                var point = content.Point;
                if (content.Direction == (int)GamePointTransactionType.ADVANCE)
                {
                    // 是否全攜
                    if (content.IsAllin)
                    {
                        content.Point = credit.Point;
                        credit.Point = 0;
                    }
                    else
                        credit.Point -= content.Point;

                    point = -content.Point;
                }
                else if (content.Direction == (int)GamePointTransactionType.RETURN)
                    credit.Point += content.Point;

                var result = sqlSugar.Updateable(credit)
                    .With(SqlWith.HoldLock)
                    .With(SqlWith.UpdLock)
                    .ExecuteCommand();
                if (result != 1)
                    return MessageCode.UNEXPECTED_ERROR;

                // Insertable PointUpdateHistory
                var time = DateTime.UtcNow;
                var history = new PointUpdateHistory
                {
                    MemberID = content.MemberID,
                    TypeID = (int)TransactionType.PREPAY_POINT,
                    PointType = content.PointType,
                    BeforePoint = beforePoint,
                    Point = point,
                    AfterPoint = credit.Point,
                    BuildDateTime = time,
                    Comment = content.AccessSerial
                };

                result = sqlSugar.Insertable<PointUpdateHistory>(history)
                    .With(SqlWith.HoldLock)
                    .With(SqlWith.UpdLock)
                    .ExecuteCommand();
                if (result != 1)
                    return MessageCode.UNEXPECTED_ERROR;

                // Insertable GamePointTransaction
                trans = new GamePointTransaction
                {
                    Serial = content.AccessSerial,
                    MemberID = content.MemberID,
                    MemberOnlineToken = content.MemberOnlineToken,
                    GameTicket = content.GameTicket,
                    Direction = content.Direction,
                    PointType = content.PointType,
                    BeforePoint = beforePoint,
                    Point = content.Point,
                    AfterPoint = credit.Point,
                    BuildDateTime = time
                };

                result = sqlSugar.Insertable<GamePointTransaction>(trans)
                    .With(SqlWith.HoldLock)
                    .With(SqlWith.UpdLock)
                    .ExecuteCommand();
                if (result != 1)
                    return MessageCode.UNEXPECTED_ERROR;

                return MessageCode.SUCCESS;
            }
        }

        public GamePointTransaction QueryLastGamePointTransaction(int memberID, string gameTicket)
        {
            using (var sqlSugar = new SqlSugarClient(connConfig))
            {
                // Queryable GamePointTransaction
                return sqlSugar.Queryable<GamePointTransaction>()
                        .Where(x => x.MemberID == memberID)
                        .Where(x => x.Direction == (int)GamePointTransactionType.ADVANCE)
                        .Where(x => x.GameTicket == gameTicket)
                        .OrderBy(x => x.BuildDateTime, OrderByType.Desc)
                        .First();
            }
        }
    }
}
