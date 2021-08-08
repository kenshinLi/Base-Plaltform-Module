using CommonLib.Define;
using CommonLib.Extension;
using CommonLib.Factory;
using CommonLib.Model;
using CommonLib.Service;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Platform.DAOLib.Model.DB;
using Platform.ServiceLib.Define;
using Platform.ServiceLib.DTO;
using Platform.ServiceLib.DTO.AgentService;
using Platform.ServiceLib.DTO.RequestBody;
using Platform.ServiceLib.Helper;
using Platform.ServiceLib.Model.LoginServeice;
using System.Linq;

namespace Platform.ServiceLib.Service
{
    public class AgentService : BaseInfoCommandService<AgentAuthToken, AgentServiceCommandID, AgentServiceRequestBody>
    {
        #region Property

        internal AgentService()
        {

        }

        protected override void InitializeCommadHandlers()
        {
            // 建立帳號 (Create account)
            AddCommandHandler<CreateAccountContent>(AgentServiceCommandID.CREATE_ACCOUNT, CreateAccount, ExecQueueMode.COMMAND);
        }

        #endregion Property

        #region Method

        public new object Execute(AgentServiceRequestBody body)
        {
            //檢查憑證
            var auth = AuthHelper.GetAgentAuthToken().Where(x => x.Token == body.Token).SingleOrDefault();
            if (auth == null)
            {
                logger.Info("reqGuid:{0} GetAgentAuthToken = {1} [ILLEGAL_INPUT]", body.ReqGUID, body.Token);
                return new BaseResponse
                {
                    MessageCode = (int)MessageCode.ILLEGAL_INPUT,
                    Message = MessageCode.ILLEGAL_INPUT.ToString()
                };
            }

            //檢查憑證
            var agent = AgentHelper.GetAgent(auth.AgentID);
            if (agent == null || agent.IsEnable == false)
            {
                logger.Info("reqGuid:{0} GetAgent = {1} [ILLEGAL_INPUT]", body.ReqGUID, body.Token);
                return new BaseResponse
                {
                    MessageCode = (int)MessageCode.ILLEGAL_INPUT,
                    Message = MessageCode.ILLEGAL_INPUT.ToString()
                };
            }

            return base.Execute(body, auth);
        }

        #endregion

        #region Command

        private object CreateAccount(ExecuteInfoBody<AgentAuthToken, CreateAccountContent> body)
        {                       
            // CREATE MEMBER
            var reqInfo = new ExecHttpReqInfo
            {
                HttpMethod = HttpRequestAction.POST,
                Data = new BaseRequestBody
                {
                    CommandID = (int)MemberServiceCommandID.CREATE_ACCOUNT,
                    Content = new
                    {
                        body.Info.AgentID,
                        body.Content.AccountName
                    },
                    ReqGUID = body.ReqGUID
                },
                ReqGUID = body.ReqGUID
            };

            var rsp = WebApiFactory.Instance.Excute<ServiceType, ResponseMessage>(ServiceType.MEMBER_SERVICE, reqInfo);
            if (rsp.MessageCode != (int)MessageCode.SUCCESS)
            {
                return new
                {
                    MessageCode = MessageCode.FAILDED,
                    Message = MessageCode.FAILDED.ToString()
                };
            }

            var json = JsonConvert.DeserializeObject<JObject>(rsp.Content.ToString());

            reqInfo = new ExecHttpReqInfo
            {
                HttpMethod = HttpRequestAction.POST,
                URL = "http://localhost:8080/",
                Data = new
                {
                    MembetID = json["MembetID"]
                },
                ReqGUID = body.ReqGUID
            };

            var loginRsp = WebApiFactory.Instance.Excute<ServiceType, LoginResponse>(ServiceType.LOGIN_SERVICE, reqInfo);
            if(loginRsp.Code != 1000) // SUCCESS
            {
                return new
                {
                    MessageCode = MessageCode.FAILDED,
                    Message = MessageCode.FAILDED.ToString()
                };
            }

            return new
            {
                MessageCode = MessageCode.SUCCESS,
                Message = MessageCode.SUCCESS.ToString(),
                LoginToken = loginRsp.LoginToken
            };
        }
        #endregion
    }
}
