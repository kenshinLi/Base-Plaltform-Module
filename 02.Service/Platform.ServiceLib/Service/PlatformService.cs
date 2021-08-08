
using CommonLib.Define;
using CommonLib.Interface;
using CommonLib.Model;
using CommonLib.Service;
using PlatformSystem.ServiceLib.Define;
using PlatformSystem.ServiceLib.Model.Common;
using PlatformSystem.ServiceLib.Model.Platform;
using System;
using PlatformSystem.DAOLib.Factory;
using PlatformSystem.ServiceLib.Helper;
using PlatformSystem.DAOLib.Defines;

namespace PlatformSystem.ServiceLib.Service
{
    public class PlatformService : BaseCommandService<PlatformServiceCommandID, BaseRequestBody>
    {
        #region Property

        internal PlatformService()
        {

        }

        protected override void InitializeCommadHandlers()
        {
            // 試玩登入 (Demo play login)
            AddCommandHandler<DemoPlayLoginContent>(PlatformServiceCommandID.DEMO_PLAY_LOGIN, DemoPlayLogin);
            // 查詢投注紀錄 (Query wager)
            AddCommandHandler<QueryWagerContent>(PlatformServiceCommandID.QUERY_WAGER, QueryWager);
        }

        #endregion Property

        #region Command

        // 試玩登入 (Demo play login)
        public IResponseMessage DemoPlayLogin(ExecuteBody<DemoPlayLoginContent> body)
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
                    MessageCode = (int)MessageCode.ILLEGAL_INPUT,
                    Message = MessageCode.ILLEGAL_INPUT.ToString()
                };
            }

            var result = DAOFactory.Common.DemoPlayLogin(body.Content.Code);
            if (result == null)
            {
                logger.Info("reqGuid:{0} DemoPlayLogin [ILLEGAL_INPUT]", body.ReqGUID);

                return new ResponseMessage()
                {
                    MessageCode = (int)MessageCode.ILLEGAL_INPUT,
                    Message = MessageCode.ILLEGAL_INPUT.ToString()
                };
            }

            return new ResponseMessage()
            {
                MessageCode = (int)MessageCode.SUCCESS,
                Content = new
                {
                    result.GameID,
                    result.GroupID,
                    result.Language
                }
            };
        }

        // 查詢投注紀錄 (Query wager)
        public IResponseMessage QueryWager(ExecuteBody<QueryWagerContent> body)
        {
            if (string.IsNullOrEmpty(body.Content.Serial))
            {
                logger.Info("reqGuid:{0} Serial [ILLEGAL_INPUT]", body.ReqGUID);

                return new ResponseMessage
                {
                    MessageCode = (int)MessageCode.ILLEGAL_INPUT,
                    Message = MessageCode.UNEXPECTED_ERROR.ToString()
                };
            }

            var result = DAOFactory.Client.QueryWager(body.Content.Serial);
            if(result == null)
            {
                logger.Info("reqGuid:{0} QueryWager [ILLEGAL_INPUT]", body.ReqGUID);

                return new ResponseMessage()
                {
                    MessageCode = (int)MessageCode.ILLEGAL_INPUT
                };
            }

            return new ResponseMessage()
            {
                MessageCode = (int)MessageCode.SUCCESS,
                Content = result
            };
        }

        #endregion
    }
}
