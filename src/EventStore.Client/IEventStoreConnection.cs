using EventStore.Client.Operations;
using System.Threading.Tasks;

namespace EventStore.Client
{
    public interface IEventStoreConnection
    {
        Task<OperationResult> Connect();
    }
}
