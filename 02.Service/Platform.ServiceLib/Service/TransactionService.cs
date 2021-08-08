
using CommonLib.Define;
using CommonLib.Extension;
using CommonLib.Interface;
using CommonLib.Model;
using CommonLib.Service;
using PlatformSystem.DAOLib.Defines;
using PlatformSystem.DAOLib.DTO.Transaction;
using PlatformSystem.DAOLib.Model;
using PlatformSystem.ServiceLib.Define;
using PlatformSystem.ServiceLib.Model.Agent;
using PlatformSystem.ServiceLib.Model.TransactionService;
using Newtonsoft.Json;
using System;
using PlatformSystem.DAOLib.Factory;
using PlatformSystem.ServiceLib.Helper;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlatformSystem.ServiceLib.Service
{
    public class TransactionService : BaseCommandService<TransactionServiceCommandID, BaseRequestBody>
    {
        #region Property

        internal TransactionService()
        {

        }

        protected override void InitializeCommadHandlers()
        {            
            // 發起開洗分 (Cash in out)
            AddCommandHandler<InitiateCashInOutDTO>(TransactionServiceCommandID.INITIATE_CASH_IN_OUT, InitiateCashInOut, true);
            // 開洗分 (Cash in out)
            AddCommandHandler<CashInOutContent>(TransactionServiceCommandID.CASH_IN_OUT, CashInOut, true);

            // 遊戲點數異動 (UpdateGamePoint)
            AddCommandHandler<UpdateGamePointContent>(TransactionServiceCommandID.UPDATE_GAME_POINT, UpdateGamePoint, true);
        }

        #endregion Property

        #region Command

        // 開洗分 (Cash in out)
        private IResponseMessage InitiateCashInOut(ExecuteBody<InitiateCashInOutDTO> body)
        {
            // GET CREDIT
            var credit = DAOFactory.Member.GetMemberCredit(body.Content.MemberID);
            if (credit == null)
            {
                logger.Info("reqGuid:{0} GetMemberCredit MemberID error [ILLEGAL_INPUT]", body.ReqGUID);

                return new ResponseMessage
                {
                    MessageCode = (int)MessageCode.ILLEGAL_INPUT,
                    Message = MessageCode.ILLEGAL_INPUT.ToString()
                };
            }

            if (string.IsNullOrEmpty(body.Content.AccessCode))
            {
                logger.Info("reqGuid:{0} AccessCode [ILLEGAL_INPUT]", body.ReqGUID);

                return new ResponseMessage
                {
                    MessageCode = (int)MessageCode.ILLEGAL_INPUT,
                    Message = MessageCode.ILLEGAL_INPUT.ToString()
                };
            }

            if (body.Content.IsCashOutAll == false && body.Content.Point <= 0)
            {
                logger.Info("reqGuid:{0} Point [ILLEGAL_INPUT]", body.ReqGUID);

                return new ResponseMessage
                {
                    MessageCode = (int)MessageCode.ILLEGAL_INPUT,
                    Message = MessageCode.ILLEGAL_INPUT.ToString()
                };
            }

            // CashOutAll
            if (body.Content.IsCashOutAll)
            {
                body.Content.Direction = (int)CashInOutType.OUT;
                body.Content.Point = 0;
            }
            else
            {
                if (Enum.TryParse(body.Content.Direction.ToString(), out CashInOutType direction) == false)
                {
                    logger.Info("reqGuid:{0} Direction error [ILLEGAL_INPUT]", body.ReqGUID);

                    return new ResponseMessage
                    {
                        MessageCode = (int)MessageCode.ILLEGAL_INPUT,
                        Message = MessageCode.ILLEGAL_INPUT.ToString()
                    };
                }

                // 洗分 餘額不足
                if (direction == CashInOutType.OUT)
                {
                    if (credit.Point < body.Content.Point)
                    {
                        logger.Info("reqGuid:{0} credit:{1} [ILLEGAL_INPUT]", body.ReqGUID, credit.Point);

                        return new ResponseMessage
                        {
                            MessageCode = (int)MessageCode.ILLEGAL_INPUT,
                            Message = MessageCode.ILLEGAL_INPUT.ToString()
                        };
                    }

                    body.Content.Point = (-body.Content.Point);
                }
            }

            // InitiateCashInOut
            body.Content.PointType = credit.PointType;
            
            var messageCode = DAOFactory.Transaction.InitiateCashInOut(body.Content, out CashInOutTransaction transaction);
            if (messageCode == MessageCode.SUCCESS)
            {
                return new ResponseMessage
                {
                    Content = transaction,
                    MessageCode = (int)MessageCode.SUCCESS,
                    Message = MessageCode.SUCCESS.ToString()
                };
            }
            else
                logger.Warn("reqGuid:{0} InitiateCashInOut [{1}]", body.ReqGUID, messageCode.ToString());

            return new ResponseMessage
            {
                MessageCode = (int)messageCode,
                Message = messageCode.ToString()
            };
        }

        // 開洗分 (Cash in out)
        private IResponseMessage CashInOut(ExecuteBody<CashInOutContent> body)
        {
            // QUERY
            var messageCode = DAOFactory.Transaction.QueryCashInOut(new QueryCashInOutDTO().Mapper(body.Content), out CashInOutTransaction transaction);
            if (messageCode != MessageCode.SUCCESS)
            {
                logger.Info("reqGuid:{0} QueryCashInOut [{1}]", body.ReqGUID, messageCode.ToString());

                return new ResponseMessage
                {
                    MessageCode = (int)messageCode,
                    Message = messageCode.ToString()
                };
            }
            else
                logger.Info("reqGuid:{0} QueryCashInOut:{1}", body.ReqGUID, JsonConvert.SerializeObject(transaction));

            // GET CREDIT
            var credit = DAOFactory.Member.GetMemberCredit(transaction.MemberID);
            if (credit == null)
            {
                logger.Info("reqGuid:{0} GetMemberCredit [UNEXPECTED_ERROR]", body.ReqGUID);

                return new ResponseMessage
                {
                    MessageCode = (int)MessageCode.UNEXPECTED_ERROR,
                    Message = MessageCode.UNEXPECTED_ERROR.ToString()
                };
            }

            //GET Direction PointType
            if (Enum.TryParse(transaction.Direction.ToString(), out CashInOutType direction) == false)
            {
                logger.Info("reqGuid:{0} Direction [UNEXPECTED_ERROR]", body.ReqGUID, transaction.Direction);

                return new ResponseMessage
                {
                    MessageCode = (int)MessageCode.UNEXPECTED_ERROR,
                    Message = MessageCode.UNEXPECTED_ERROR.ToString()
                };
            }

            // Check Point
            if (direction == CashInOutType.OUT && credit.Point - transaction.Point < 0)
            {
                logger.Info("reqGuid:{0} credit:{1} [DENY_ACCESS]", body.ReqGUID, credit.Point);

                return new ResponseMessage
                {
                    MessageCode = (int)MessageCode.DENY_ACCESS,
                    Message = MessageCode.DENY_ACCESS.ToString()
                };
            }

            // Updateable MemberCredit
            if (transaction.IsCashOutAll)
                transaction.Point = credit.Point;

            var beforePoint = credit.Point;
            var point = transaction.Direction == (int)CashInOutType.OUT ? -(transaction.Point) : transaction.Point;

            credit.Point += point;

            Object content = null;
            messageCode = DAOFactory.Transaction.UpdateMemberCredit(credit);
            if (messageCode == MessageCode.SUCCESS)
            {
                // CashInOut
                var dto = new CashInOutDTO
                {
                    BeforePoint = beforePoint,
                    Point = point,
                    AfterPoint = credit.Point,
                    PointType = credit.PointType
                };

                messageCode = DAOFactory.Transaction.CashInOut(dto, ref transaction);
                if (messageCode == MessageCode.SUCCESS)
                {
                    content = transaction;

                    // Check member is gaming
                    var ticket = GameHelper.GetGameTicket(transaction.MemberID);
                    if (ticket == null || ticket.IsFinished)
                        TransactionHelper.UpdateMemberDisplayPoint(credit);
                    else if (TransactionHelper.GetMemberDisplayPoint(transaction.MemberID, out MemberCredit cacheCredit) == MessageCode.SUCCESS)
                    {
                        cacheCredit.Point += point;
                        TransactionHelper.UpdateMemberDisplayPoint(cacheCredit);
                    }
                }
                else
                    logger.Warn("reqGuid:{0} CashInOut [UNEXPECTED_ERROR]", body.ReqGUID);
            }

            return new ResponseMessage
            {
                MessageCode = (int)messageCode,
                Content = content
            };
        }

        // 遊戲點數異動 (UpdateGamePoint)
        private IResponseMessage UpdateGamePoint(ExecuteBody<UpdateGamePointContent> body)
        {
            if(string.IsNullOrEmpty(body.Content.AccessSerial))
            {
                return new ResponseMessage
                {
                    MessageCode = (int)MessageCode.ILLEGAL_INPUT,
                    Message = MessageCode.ILLEGAL_INPUT.ToString()
                };
            }
            var trans = DAOFactory.Transaction.QueryGamePointBySerial(body.Content.AccessSerial);
            if(trans != null)
            {
                return new ResponseMessage
                {
                    MessageCode = (int)MessageCode.HAS_FINISHED,
                    Message = MessageCode.HAS_FINISHED.ToString()
                };
            }

            //GET PointType
            if (Enum.TryParse(body.Content.PointType.ToString(), out PointType currency) == false)
            {
                logger.Info("reqGuid:{0} PointType error [ILLEGAL_INPUT]", body.ReqGUID);

                return new ResponseMessage
                {
                    MessageCode = (int)MessageCode.ILLEGAL_INPUT,
                    Message = MessageCode.ILLEGAL_INPUT.ToString()
                };
            }

            // Queryable MemberCredit
            var credit = DAOFactory.Member.GetMemberCredit(body.Content.MemberID);
            if (credit == null)
            {
                logger.Fatal("reqGuid:{0} GetMemberCredit [UNEXPECTED_ERROR]", body.ReqGUID);

                return new ResponseMessage
                {
                    MessageCode = (int)MessageCode.UNEXPECTED_ERROR,
                    Message = MessageCode.UNEXPECTED_ERROR.ToString()
                };
            }

            // UpdateMemberCredit
            var beforePoint = credit.Point;

            if (body.Content.Direction == (int)GamePointTransactionType.ADVANCE)
            {
                if (credit.Point < body.Content.Point)
                {
                    logger.Info("reqGuid:{0} Point [NOT_ENOUGH]", body.ReqGUID);

                    return new ResponseMessage
                    {
                        MessageCode = (int)MessageCode.NOT_ENOUGH,
                        Message = MessageCode.NOT_ENOUGH.ToString()
                    };
                }
                else if (body.Content.IsAllin)
                    body.Content.Point = (-credit.Point);
                else
                    body.Content.Point = -(body.Content.Point);
            }

            credit.Point += body.Content.Point;

            // UpdateGamePoint
            var dto = new UpdateGamePointDTO
            {
                BeforePoint = beforePoint,
                AfterPoint = credit.Point
            };

            dto.Mapper(body.Content);

            var messageCode = DAOFactory.Transaction.UpdateGamePoint(dto, out Object content);
            if (messageCode == MessageCode.SUCCESS && content != null)
            {
                messageCode = DAOFactory.Transaction.UpdateMemberCredit(credit);
                if (messageCode != MessageCode.SUCCESS)
                    content = null;
            }
            
            return new ResponseMessage
            {
                MessageCode = (int)messageCode,
                Content = content
            };            
        }
        #endregion
    }
}
