using Platform.ServiceLib.Service;
using System;

namespace Platform.ServiceLib.Factory
{
    public class ServiceFactory
    {
        private static Lazy<AgentService> _Agent { get; }

        public static AgentService Agent { get { return _Agent.Value; } }

        static ServiceFactory()
        {
            _Agent = new Lazy<AgentService>(() => new AgentService());
        }
    }
}
