using EventStore.Client.Common.Log;
using System;
using System.Collections.Generic;
using System.Text;

namespace EventStore.Client
{
    public class EventStoreConnectionSettings
    {
        public ILogger Logger;
        public EventStoreConnectionSettings()
        {
            Logger = new ConsoleLogger();
        }
        public static EventStoreConnectionSettings Default => new EventStoreConnectionSettings();
    }
}
