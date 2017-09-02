using EventStore.Client.Messages;
using System.Threading.Tasks;

namespace EventStore.Client
{
    public interface IEventStoreConnection
    {
        Task<Result> Connect();
    }
}
