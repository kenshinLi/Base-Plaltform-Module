using CommonLib.Service;
using CommonLib.Define;
using Platform.DAOLib.DAO;
using Platform.DAOLib.Defines;
using System;
using CommonLib.Utility;

namespace Platform.DAOLib.Factory
{
    public class DAOFactory
    {
        #region PLATFORM
        private static Lazy<BaseDAO> _Base { get; }
        private static Lazy<AgentDAO> _Agent { get; }

        public static BaseDAO Base { get { return _Base.Value; } }
        public static AgentDAO Agent { get { return _Agent.Value; } }
        #endregion

        static DAOFactory()
        {
            var connections = AppSettingService.Instace.ConnectionStrings;

            // PLATFORM
            var platformKey = DataBaseConnectionType.PLATFORM.ToString();
            if (connections.ContainsKey(platformKey))
            {
                var connectionString = connections[platformKey].MasterConnectionString;
                if (string.IsNullOrEmpty(connectionString))
                    throw new Exception(string.Format("ConnectionString is null: {0}", platformKey));

                var dbConnectInfo = connections[platformKey];
                _Base = new Lazy<BaseDAO>(() => new BaseDAO(dbConnectInfo));
                _Agent = new Lazy<AgentDAO>(() => new AgentDAO(dbConnectInfo));
           }
        }
    }
}
