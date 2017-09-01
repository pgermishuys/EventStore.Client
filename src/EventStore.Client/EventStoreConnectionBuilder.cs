using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace EventStore.Client
{
    public class EventStoreConnectionBuilder
    {
        public static IEventStoreConnection Create(IPEndPoint tcpEndpoint, EventStoreConnectionSettings settings)
        {
            return new EventStoreConnection(tcpEndpoint, settings);
        }
    }
}
