using CommonLib.Utility;
using Platform.DAOLib.Factory;
using Platform.DAOLib.Model.DB;
using Platform.ServiceLib.Define;

namespace Platform.ServiceLib.Helper
{
    public class AgentHelper : BaseCache<RedisCacheDefine>
    {
        #region Method
        public static Agent GetAgent(int agentID)
        {
            var key = agentID.ToString();

            //GET REDIS
            var redis = RedisDict[RedisCacheDefine.AGENT];
            if (redis.TryGet(key, out Agent agent) == false)
            {
                // GET DB
                agent = DAOFactory.Agent.GetAgent(agentID);
                if (agent != null)
                    UpdateRedis(key, agent, redis);
            }

            return agent;
        }
        #endregion
    }
}
