
using CommonLib.Define;
using CommonLib.Extension;
using CommonLib.Interface;
using CommonLib.Model;
using CommonLib.Service;
using PlatformSystem.DAOLib.DTO.Lobby;
using PlatformSystem.DAOLib.Model;
using PlatformSystem.ServiceLib.Define;
using PlatformSystem.ServiceLib.Model.Agent;
using PlatformSystem.ServiceLib.Model.Client;
using PlatformSystem.ServiceLib.Model.Member;
using PlatformSystem.ServiceLib.Model.RequestBody;
using PlatformSystem.ServiceLib.Helper;
using Newtonsoft.Json;
using System;
using System.Linq;
using PlatformSystem.DAOLib.Factory;
using PlatformSystem.ServiceLib.Model.TransactionService;
using System.Collections.Generic;
using PlatformSystem.DAOLib.Defines;

namespace PlatformSystem.ServiceLib.Service
{
    public class LobbyService : BaseInfoCommandService<MemberOnlineToken, LobbyServiceCommandID, LobbyServiceRequestBody>
    {
        #region Property
        private List<LobbyServiceCommandID> NoneTokenCommandIDList = new List<LobbyServiceCommandID>();

        internal LobbyService()
        {
            NoneTokenCommandIDList.Add(LobbyServiceCommandID.AGENT_MEMBER_LOGIN);
        }

        protected override void InitializeCommadHandlers()
        {
            AddCommandHandler<AgentMemberLoginContent>(LobbyServiceCommandID.AGENT_MEMBER_LOGIN, AgentMemberLogin);

            #region 會員端 - 會員
            // 取得會員資訊 (GetMemberInfo)
            AddCommandHandler<object>(LobbyServiceCommandID.GET_MEMBER_INFO, GetMemberInfo);
            #endregion

            #region 會員端 - 會員交易
            // 取得會員點數資訊 (Get member credit)
            AddCommandHandler<object>(LobbyServiceCommandID.GET_MEMBER_CREDIT, GetMemberCredit);
            #endregion

            #region 會員端 - 遊戲
            // 取得允許遊戲群組 (Get allow game group)
            AddCommandHandler<GetAllowGameGroupContent>(LobbyServiceCommandID.GET_ALLOW_GAME_GROUP, GetAllowGameGroup);
            #endregion            

            #region 會員端 - 會員遊戲紀錄
            // 取得投注紀錄頁 (Get wager page)
            AddCommandHandler<GetWagerPageContent>(LobbyServiceCommandID.GET_WAGER_PAGE, GetWagerPage);
            #endregion
        }

        #endregion Property

        #region Method

        public override IResponseMessage Execute(LobbyServiceRequestBody body)
        {
            //Check Command
            LobbyServiceCommandID commandID;
            if (Enum.TryParse(body.CommandID.ToString(), out commandID) == false || ContainsCommand(commandID) == false)
            {
                logger.Info("reqGuid:{0} CommandID [NONEXISTENT_FUNCTION]", body.ReqGUID);

                return new ResponseMessage
                {
                    MessageCode = (int)MessageCode.NONEXISTENT_FUNCTION
                };
            }

            if (NoneTokenCommandIDList.Contains(commandID))
                return base.Execute(body);

            // 取得Token
            var messageCode = CenterAuthHelper.GetMemberOnlineToken(body.Token, body.ReqGUID, out MemberOnlineToken memberToken);
            if (messageCode != MessageCode.SUCCESS)
            {
                logger.Info("reqGuid:{0} GetMemberOnlineToken [{1}]", body.ReqGUID, messageCode.ToString());

                return new ResponseMessage
                {
                    MessageCode = (int)messageCode
                };
            }            

            // ExecuteCommand            
            var rst = new ExecuteInfoBody<MemberOnlineToken, object>
            {
                CommandID = body.CommandID,
                Content = body.Content,
                Info = memberToken,
                ReqGUID = body.ReqGUID
            };

            var excuteResult = ExecuteCommand(commandID, rst);
            if (excuteResult != null)
                return excuteResult;
            else
            {
                logger.Info("reqGuid:{0} ExecuteCommand [UNEXPECTED_ERROR]", body.ReqGUID);

                return new ResponseMessage
                {
                    MessageCode = (int)MessageCode.UNEXPECTED_ERROR
                };
            }
        }

        public IResponseMessage AgentMemberLogin(ExecuteBody<AgentMemberLoginContent> body)
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

            if (string.IsNullOrEmpty(body.Content.Code))
            {
                logger.Info("reqGuid:{0} Code [ILLEGAL_INPUT]", body.ReqGUID);

                return new ResponseMessage
                {
                    MessageCode = (int)MessageCode.ILLEGAL_INPUT
                };
            }

            // VERIFY_AGENT_MEMBER_LOGIN_VERIFICATION           
            var rst = new BaseRequestBody
            {
                CommandID = (int)MemberServiceCommandID.VERIFY_AGENT_MEMBER_LOGIN_VERIFICATION,
                Content = new VerifyAgentMemberLoginVerificationContent
                {
                    Serial = body.Content.Code
                },
                ReqGUID = body.ReqGUID
            };

            var rsp = WebAPIService<GamePlatformServiceType>.Instance.Excute(GamePlatformServiceType.MEMBER_SERVICE, rst, out AgentMemberLoginVerification verification);
            if (rsp.MessageCode != (int)MessageCode.SUCCESS)
                return rsp;

            //GET MEMBER
            var member = MemberHelper.GetMember(new GetMemberContent { MemberID = verification.MemberID });
            if (member == null)
            {
                logger.Warn("reqGuid:{0} GetMemberByVerification:{1} Serial:{2}", body.ReqGUID, verification.MemberID, verification.Serial);

                return new ResponseMessage
                {
                    MessageCode = (int)MessageCode.UNEXPECTED_ERROR
                };
            }

            // 取得Token
            var memberToken = ClientHelper.GenerateMemberOnlineToken(member.MemberID, member.AccountName, body.ReqGUID);
            if (memberToken == null)
            {
                logger.Warn("reqGuid:{0} CheckMemberToken:{1} [UNEXPECTED_ERROR]", body.ReqGUID, member.AccountName);

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
                    MemberOnlineToken = memberToken.Token,
                    verification.Action,
                    Detail = JsonConvert.DeserializeObject<AgentMemberLoginVerificationDetail>(verification.Detail)
                }
            };
        }
        #endregion

        #region Command

        #region 會員端 - 會員

        // 取得會員資訊 (GetMemberInfo)
        private IResponseMessage GetMemberInfo(ExecuteInfoBody<MemberOnlineToken, object> body)
        {
            var result = new ResponseMessage();
            result.MessageCode = (int)MessageCode.ILLEGAL_TOKEN;
           
            var member = MemberHelper.GetMember(new GetMemberContent { MemberID = body.Info.MemberID });
            if(member != null)
            {
                result.Content = new
                {
                    member.AccountName,
                    member.UID,
                    member.IMParticipantCode,
                    member.NickName,
                    member.Avatar
                };
                result.MessageCode = (int)MessageCode.SUCCESS;
            }
            else
                logger.Warn("reqGuid:{0} GetMember:{1} [UNEXPECTED_ERROR]", body.ReqGUID, body.Info.AccountName);

            return result;            
        }

        #endregion

        #region 會員端 - 遊戲

        // 取得允許遊戲 (Get allow game)
        private IResponseMessage GetAllowGameGroup(ExecuteInfoBody<MemberOnlineToken, GetAllowGameGroupContent> body)
        {
            //GET MEMBER
            var member = MemberHelper.GetMember(new GetMemberContent { MemberID = body.Info.MemberID });
            if (member == null)
            {
                logger.Warn("reqGuid:{0} GetMember:{1} [DENY_ACCESS]", body.ReqGUID, body.Info.AccountName);

                return new ResponseMessage
                {
                    MessageCode = (int)MessageCode.DENY_ACCESS
                };
            }

            // GetAllowGameGroup
            body.Content.AgentID = member.AgentID;
            body.Content.IsEnable = true;

            var list = AgentHelper.GetAllowGameGroup(body.Content);
  
            return new ResponseMessage
            {
                MessageCode = (int)MessageCode.SUCCESS,
                Content = new
                {
                    List = list.Select(x => new
                    {
                        x.GameID,
                        x.GroupID
                    })
                }                
            };
        }

        #endregion

        #region 會員端 - 會員交易
        // 取得會員點數資訊 (Get member credit)
        private IResponseMessage GetMemberCredit(ExecuteInfoBody<MemberOnlineToken, object> body)
        {
            //GET MEMBER
            var member = MemberHelper.GetMember(new GetMemberContent { MemberID = body.Info.MemberID });
            if (member == null)
            {
                logger.Error("reqGuid:{0} GetMember error [UNEXPECTED_ERROR]", body.ReqGUID);

                return new ResponseMessage
                {
                    MessageCode = (int)MessageCode.UNEXPECTED_ERROR,
                    Message = MessageCode.UNEXPECTED_ERROR.ToString()
                };
            }

            // GetMemberDisplayPoint
            var messageCode = TransactionHelper.GetMemberDisplayPoint(member.MemberID, out MemberCredit credit);
            if (messageCode != MessageCode.SUCCESS)
            {
                return new ResponseMessage
                {
                    MessageCode = (int)MessageCode.DENY_ACCESS,
                    Message = MessageCode.DENY_ACCESS.ToString()
                };
            }

            return new ResponseMessage
            {
                MessageCode = (int)MessageCode.SUCCESS,
                Content = new
                {
                    credit.PointType,
                    credit.Point
                }
            };
        }

        #endregion

        #region 會員端 - 會員遊戲紀錄
        // 取得投注紀錄頁 (Get wager page)
        public IResponseMessage GetWagerPage(ExecuteInfoBody<MemberOnlineToken, GetWagerPageContent> body)
        {
            if (body.Content.Count  <= 0 ||
                body.Content.Count > 1000 )
            {
                logger.Info("reqGuid:{0} Count [ILLEGAL_INPUT]", body.ReqGUID);

                return new ResponseMessage
                {
                    MessageCode = (int)MessageCode.ILLEGAL_INPUT,
                    Message = MessageCode.ILLEGAL_INPUT.ToString()
                };
            }

            // GetWagerPage
            var dto = new GetWagerPageDTO();
            dto.Mapper(body.Content);
            dto.MemberID = body.Info.MemberID;

            var wagerList = DAOFactory.Client.GetWagerPage(dto);

            return new ResponseMessage()
            {
                MessageCode = (int)MessageCode.SUCCESS,
                Content = wagerList
            };
        }
        #endregion
        #endregion
    }
}
