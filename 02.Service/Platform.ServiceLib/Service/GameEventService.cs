
using CommonLib.Define;
using CommonLib.Interface;
using CommonLib.Model;
using CommonLib.Service;
using PlatformSystem.DAOLib.Defines;
using PlatformSystem.ServiceLib.Define;
using PlatformSystem.ServiceLib.Model.PlayerGameService;
using PlatformSystem.ServiceLib.Model.RequestBody;
using PlatformSystem.ServiceLib.Helper;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;
using PlatformSystem.DAOLib.Factory;
using PlatformSystem.DAOLib.DTO.Game;
using CommonLib.Extension;
using System.Collections.Generic;
using PlatformSystem.DAOLib.Model;

namespace PlatformSystem.ServiceLib.Service
{
    public class GameEventService : BaseCommandService<GameEventServiceCommandID, BaseRequestBody>
    {
        #region Property
        internal GameEventService()
        {

        }

        protected override void InitializeCommadHandlers()
        {
            // 通知玩家已經離開平台 (Notify player has leaved platform)
            AddCommandHandler<NotifyPlayerHasLeavedPlatformContent>(GameEventServiceCommandID.NOTIFY_PLAYER_HAS_LEAVED_PLATFORM, NotifyPlayerHasLeavedPlatform);
            // 通知玩家有個開分 (Notify player has a cash in)
            AddCommandHandler<GameEventContent>(GameEventServiceCommandID.NOTIFY_PLAYER_HAS_A_CASH_IN, NotifyPlayerHasACashIn);
        }

        #endregion Property

        #region Method
       
        public IResponseMessage Execute(GameEventServiceRequestBody body)
        {
            var result = new ResponseMessage();

            //Check Command
            GameEventServiceCommandID commandID;
            if (Enum.TryParse(body.CommandID.ToString(), out commandID) == false || ContainsCommand(commandID) == false)
            {
                result.MessageCode = (int)MessageCode.NONEXISTENT_FUNCTION;
                return result;
            }

            Task.Run(() =>
            {
                // ExecuteCommand
                var rst = new ExecuteBody<object>
                {
                    CommandID = body.CommandID,
                    Content = body.Content,
                    ReqGUID = body.ReqGUID
                };
                var excuteResult = ExecuteCommand(commandID, rst) as IResponseMessage;
                if (excuteResult.MessageCode != (int)MessageCode.SUCCESS)
                    logger.Warn(JsonConvert.SerializeObject(excuteResult));
            });

            return new ResponseMessage
            {
                MessageCode = (int)MessageCode.SUCCESS
            };
        }
        #endregion

        #region Command

        // 通知玩家已經離開平台 (Notify player has leaved platform)
        private IResponseMessage NotifyPlayerHasLeavedPlatform(ExecuteBody<NotifyPlayerHasLeavedPlatformContent> body)
        {
            if (body.Content.List.Count == 0 ||
                Enum.TryParse(body.Content.CauseType.ToString(), out CauseType causeType) == false)
            {
                return new ResponseMessage
                {
                    MessageCode = (int)MessageCode.ILLEGAL_INPUT,
                    Message = MessageCode.ILLEGAL_INPUT.ToString()
                };
            }

            var dto = new QueryMemberGameTicketIsNotFinishedDTO
            {
                MemberOnlineTokens = body.Content.List
            };

            var messageCode = GameEventHelper.QueryMemberGameTicketIsNotFinished(dto, out List<GameTicket> tickets);
            logger.Info("reqGuid:{0} QueryMemberGameTicketIsNotFinished Count:{1}", body.ReqGUID, tickets.Count);

            if (tickets.Count == 0)
            {                
                return new ResponseMessage
                {
                    MessageCode = (int)messageCode,
                    Message = messageCode.ToString()
                };
            }

            // NOTIFY_PLAYER_HAS_LEAVED_PLATFORM
            var rst = new BaseRequestBody
            {
                CommandID = (int)GameManagerServiceCommandID.NOTIFY_PLAYER_HAS_LEAVED_PLATFORM,
                Content = new
                {
                    List = tickets.Select(x => new
                    {
                        GameTicket = x.Serial,
                        body.Content.CauseType
                    })
                },
                ReqGUID = body.ReqGUID
            };

            return WebAPIService<GamePlatformServiceType>.Instance.Excute(GamePlatformServiceType.GAME_MANAGER_SERVICE, rst);
        }

        // 通知玩家有個開分 (Notify player has a cash in)
        private IResponseMessage NotifyPlayerHasACashIn(ExecuteBody<GameEventContent> body)
        {
            var dto = new QueryMemberGameTicketIsNotFinishedDTO
            {
                MemberID = body.Content.MemberID
            };
            var messageCode = GameEventHelper.QueryMemberGameTicketIsNotFinished(dto, out List<GameTicket> tickets);
            if (tickets.Count == 0)
            {
                return new ResponseMessage
                {
                    MessageCode = (int)messageCode,
                    Message = messageCode.ToString()
                };
            }

            // NOTIFY_PLAYER_HAS_A_CASH_IN
            var rst = new BaseRequestBody
            {
                CommandID = (int)GameManagerServiceCommandID.NOTIFY_PLAYER_HAS_A_CASH_IN,
                Content = new
                {
                    GameTicket = tickets.First().Serial
                },
                ReqGUID = body.ReqGUID
            };

            return WebAPIService<GamePlatformServiceType>.Instance.Excute(GamePlatformServiceType.GAME_MANAGER_SERVICE, rst);
        }
        #endregion
    }
}
