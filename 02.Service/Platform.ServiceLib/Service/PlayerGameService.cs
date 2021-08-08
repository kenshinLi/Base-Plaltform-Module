
using CommonLib.Define;
using CommonLib.Extension;
using CommonLib.Interface;
using CommonLib.Model;
using CommonLib.Service;
using PlatformSystem.DAOLib.Defines;
using PlatformSystem.DAOLib.DTO.Game;
using PlatformSystem.DAOLib.Model;
using PlatformSystem.ServiceLib.Define;
using PlatformSystem.ServiceLib.Model.Agent;
using PlatformSystem.ServiceLib.Model.Common;
using PlatformSystem.ServiceLib.Model.Member;
using PlatformSystem.ServiceLib.Model.PlayerGameService;
using PlatformSystem.ServiceLib.Model.RequestBody;
using PlatformSystem.ServiceLib.Model.TransactionService;
using PlatformSystem.ServiceLib.Helper;
using Newtonsoft.Json;
using System;
using System.Linq;
using PlatformSystem.DAOLib.Factory;
using System.Threading;

namespace PlatformSystem.ServiceLib.Service
{
    public class PlayerGameService : BaseCommandService<PlayerGameServiceCommandID, BaseRequestBody>
    {
        #region Property

        internal PlayerGameService()
        {

        }

        protected override void InitializeCommadHandlers()
        {
            // 申請遊戲門票 (Apply for game ticket)
            AddCommandHandler<ApplyForGameTicketContent>(PlayerGameServiceCommandID.APPLY_FOR_GAME_TICKET, ApplyForGameTicket);
            // 完結遊戲門票 (Finish game ticket)
            AddCommandHandler<FinishGameTicketContent>(PlayerGameServiceCommandID.FINISH_GAME_TICKET, FinishGameTicket);
            // 取得試玩資訊 (Get Demo play info)
            AddCommandHandler<DemoPlayLoginContent>(PlayerGameServiceCommandID.GET_DEMO_PLAY_INFO, GetDemoPlayInfo);

            // 預借點數 (Advance point)
            AddCommandHandler<UpdateGamePointContent>(PlayerGameServiceCommandID.ADVANCE_POINT, AdvancePoint);

            // 歸還點數 (Return point)
            AddCommandHandler<UpdateGamePointContent>(PlayerGameServiceCommandID.RETURN_POINT, ReturnPoint);
        }

        #endregion Property

        #region Command

        // 申請遊戲門票 (Apply for game ticket)
        private IResponseMessage ApplyForGameTicket(ExecuteBody<ApplyForGameTicketContent> body)
        {
            // Maintenance
            var maintenanceState = CommonHelper.GetMaintenanceState();
            if (maintenanceState > MaintenanceState.NORMAL)
            {
                logger.Info("reqGuid:{0} Maintenance Is Enable", body.ReqGUID);

                return new ResponseMessage
                {
                    MessageCode = (int)MessageCode.ON_MAINTENANCE
                };
            }

            // CHECK TOKEN
            // 取得Token
            var messageCode = CenterAuthHelper.GetMemberOnlineToken(body.Content.MemberOnlineToken, body.ReqGUID, out MemberOnlineToken memberToken);
            if (messageCode != MessageCode.SUCCESS)
            {
                logger.Info("reqGuid:{0} GetMemberOnlineToken [{1}]", body.ReqGUID, messageCode.ToString());

                return new ResponseMessage
                {
                    MessageCode = (int)messageCode
                };
            }

            //GET MEMBER
            var member = MemberHelper.GetMember(new GetMemberContent { MemberID = memberToken.MemberID });
            if (member == null)
            {
                logger.Info("reqGuid:{0} GetMember [DENY_ACCESS]", body.ReqGUID);

                return new ResponseMessage
                {
                    MessageCode = (int)MessageCode.DENY_ACCESS
                };
            }

            // Game
            var gameInstanceList = GameHelper.GetGameInstance(body.Content.GameID, true);
            if (gameInstanceList.Count == 0)
            {
                logger.Info("reqGuid:{0} GetGameInstance [ILLEGAL_INPUT]", body.ReqGUID);

                return new ResponseMessage
                {
                    MessageCode = (int)MessageCode.ILLEGAL_INPUT
                };
            }

            //Get Agent
            var agent = AgentHelper.GetAgent(new GetAgentContent { AgentID = member.AgentID });
            if (agent == null)
            {
                logger.Info("reqGuid:{0} GetAgent [ILLEGAL_INPUT]", body.ReqGUID);

                return new ResponseMessage
                {
                    MessageCode = (int)MessageCode.ILLEGAL_INPUT,
                    Message = MessageCode.ILLEGAL_INPUT.ToString()
                };
            }

            var allowGameGroupList = AgentHelper.GetAllowGameGroup(new GetAllowGameGroupContent
            {
                AgentID = agent.AgentID,
                GameID = body.Content.GameID,
                GroupID = body.Content.GroupID,
                IsEnable = true
            });
            if(allowGameGroupList.Count == 0)
            {
                logger.Info("reqGuid:{0} GetAllowGameGroup [ILLEGAL_INPUT]", body.ReqGUID);

                return new ResponseMessage
                {
                    MessageCode = (int)MessageCode.ILLEGAL_INPUT,
                    Message = MessageCode.ILLEGAL_INPUT.ToString()
                };
            }

            // ApplyForGameTicket
            var dto = new ApplyForGameTicketDTO
            {
                MemberID = member.MemberID,
                MemberType = member.TypeID,
                PointType = agent.PointType
            };
            dto.Mapper(body.Content);

            var ticket = GameHelper.ApplyForGameTicket(dto);
            if (ticket == null)
            {
                logger.Info("reqGuid:{0} ApplyForGameTicket [UNEXPECTED_ERROR]", body.ReqGUID);

                return new ResponseMessage
                {
                    MessageCode = (int)MessageCode.UNEXPECTED_ERROR
                };
            }

            return new ResponseMessage
            {
                MessageCode = (int)MessageCode.SUCCESS,
                Content = new
                {
                    ticket.Serial,
                    member.MemberID,
                    ticket.MemberType,
                    member.AgentID,
                    agent.PointType,
                    member.UID,
                    member.AccountName,
                    member.NickName,
                    member.VIPLevel
                }
            };
        }

        // 完結遊戲門票 (Finish game ticket)
        private IResponseMessage FinishGameTicket(ExecuteBody<FinishGameTicketContent> body)
        {
            if (body.Content.GameTickets.Count() == 0)
            {
                logger.Info("reqGuid:{0} GameTickets [ILLEGAL_INPUT]", body.ReqGUID);

                return new ResponseMessage
                {
                    MessageCode = (int)MessageCode.ILLEGAL_INPUT,
                    Message = MessageCode.ILLEGAL_INPUT.ToString()
                };
            }

            var messageCode = GameHelper.FinishGameTicket(body.Content.GameTickets);
            if (messageCode == MessageCode.ILLEGAL_INPUT)
                logger.Info("reqGuid:{0} QueryGameTickets [ILLEGAL_INPUT]", body.ReqGUID);
            else if(messageCode == MessageCode.UNEXPECTED_ERROR)
                logger.Error("reqGuid:{0} UpdateGameTicket [UNEXPECTED_ERROR]", body.ReqGUID);

            return new ResponseMessage()
            {
                MessageCode = (int)messageCode,
                Message = messageCode.ToString()
            };
        }

        // 取得試玩資訊 (Get Demo play info)
        private IResponseMessage GetDemoPlayInfo(ExecuteBody<DemoPlayLoginContent> body)
        {
            if (string.IsNullOrEmpty(body.Content.Code))
            {
                logger.Info("reqGuid:{0} Code [ILLEGAL_INPUT]", body.ReqGUID);

                return new ResponseMessage
                {
                    MessageCode = (int)MessageCode.ILLEGAL_INPUT,
                    Message = MessageCode.UNEXPECTED_ERROR.ToString()
                };
            }

            var result = DAOFactory.Common.DemoPlayLogin(body.Content.Code);
            if (result == null)
            {
                logger.Info("reqGuid:{0} DemoPlayLogin [ILLEGAL_INPUT]", body.ReqGUID);

                return new ResponseMessage()
                {
                    MessageCode = (int)MessageCode.ILLEGAL_INPUT
                };
            }

            return new ResponseMessage()
            {
                MessageCode = (int)MessageCode.SUCCESS,
                Content = new
                {
                    result.DemoPlayType,
                    result.AgentID,
                    result.GameID,
                    result.GroupID,
                    result.PromotionID
                }
            };
        }

        // 預借點數 (Advance point)
        private IResponseMessage AdvancePoint(ExecuteBody<UpdateGamePointContent> body)
        {
            // Check Ticket
            var ticket = GameHelper.GetGameTicket(body.Content.GameTicket);
            if(ticket == null || 
               ticket.MemberID != body.Content.MemberID ||
               ticket.MemberOnlineToken != body.Content.MemberOnlineToken)
            {
                logger.Info("reqGuid:{0} GetGameTicket [ILLEGAL_INPUT]", body.ReqGUID);

                return new ResponseMessage
                {
                    MessageCode = (int)MessageCode.ILLEGAL_INPUT
                };
            }

            body.Content.Direction = (int)GamePointTransactionType.ADVANCE;
            body.Content.PointType = ticket.PointType;

            var rst = new TransactionServiceRequestBody
            {
                CommandID = (int)TransactionServiceCommandID.UPDATE_GAME_POINT,
                Content = body.Content,
                ReqGUID = body.ReqGUID
            };

            return WebAPIService<GamePlatformServiceType>.Instance.Excute(GamePlatformServiceType.TRANS_SERVICE, rst);
        }

        // 歸還點數 (Return point)
        private IResponseMessage ReturnPoint(ExecuteBody<UpdateGamePointContent> body)
        {
            // Check Ticket
            var ticket = GameHelper.GetGameTicket(body.Content.GameTicket);
            if (ticket == null ||
               ticket.MemberID != body.Content.MemberID ||
               ticket.MemberOnlineToken != body.Content.MemberOnlineToken)
            {
                logger.Info("reqGuid:{0} GetGameTicket [ILLEGAL_INPUT]", body.ReqGUID);

                return new ResponseMessage
                {
                    MessageCode = (int)MessageCode.ILLEGAL_INPUT
                };
            }

            body.Content.Direction = (int)GamePointTransactionType.RETURN;
            body.Content.PointType = ticket.PointType;

            var rst = new TransactionServiceRequestBody
            {
                CommandID = (int)TransactionServiceCommandID.UPDATE_GAME_POINT,
                Content = body.Content,
                ReqGUID = body.ReqGUID
            };

            return WebAPIService<GamePlatformServiceType>.Instance.Excute(GamePlatformServiceType.TRANS_SERVICE, rst);
        }
        #endregion
    }
}
