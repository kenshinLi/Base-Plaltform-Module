using CommonLib.Utility;
using Platform.DAOLib.Factory;
using Platform.DAOLib.Model.DB;
using Platform.ServiceLib.Define;
using System;
using System.Collections.Generic;

namespace Platform.ServiceLib.Helper
{
    public class AuthHelper : BaseCache
    {
        #region Method

        public static List<AgentAuthToken> GetAgentAuthToken()
        {
            var key = CacheID.AGENT_AUTH.ToString();

            Func<List<AgentAuthToken>> func = delegate () { return DAOFactory.Base.GetList<AgentAuthToken>(); };

            return AccessCache(key, func, DateTimeOffset.UtcNow.AddMinutes(10));
        }
        #endregion
    }
}
