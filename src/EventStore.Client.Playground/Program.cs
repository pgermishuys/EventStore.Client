using System;
using System.Net;
using System.Threading.Tasks;

namespace EventStore.Client.Playground
{
    public class Program
    {
        static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
            Console.ReadLine();
        }

        static async Task MainAsync(string[] args)
        {
            IEventStoreConnection connection = EventStoreConnectionBuilder.Create(new IPEndPoint(IPAddress.Loopback, 1113), EventStoreConnectionSettings.Default);
            var result = await connection.Connect();
            Console.WriteLine(result);
        }
    }
}
