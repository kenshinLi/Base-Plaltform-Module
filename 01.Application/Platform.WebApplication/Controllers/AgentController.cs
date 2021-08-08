using CommonLib.Helper;
using CommonLib.Utility;
using Platform.ServiceLib.Define;
using Platform.ServiceLib.DTO.AgentService;
using Platform.ServiceLib.DTO.RequestBody;
using Platform.ServiceLib.Factory;
using System.Web.Http;
using System.Web.Http.Cors;

namespace Platform.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class AgentController : BaseApiController
    {
        /// <summary>
        /// To create an account if the account is not existed, otherwise response fail.
        /// </summary>
        /// <param name="body"></param>
        /// <returns></returns>
        [HttpPost]
        public object CreateAccount(CreateAccountContent content)
        {
            return DoAction(AgentServiceCommandID.CREATE_ACCOUNT, content);
        }


        #region PRIVATE

        private object DoAction(AgentServiceCommandID commandID, dynamic content)
        {
            var body = new AgentServiceRequestBody
            {
                CommandID = (int)commandID,
                Token = content.Token,
                Content = content,
                ReqGUID = GuidHelper.Base64Guid(true)
            };

            return base.ExecuteFunc(body, ServiceFactory.Agent.Execute, body.ReqGUID, body.CommandID);
        }
        
        #endregion
    }
}