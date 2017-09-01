using System;
using System.Threading.Tasks;

namespace EventStore.Client.Operations
{
    public abstract class Operation
    {
        public Guid CorrelationId;
        public TaskCompletionSource<OperationResult> Source;
        public Task<OperationResult> Task => Source.Task;
        public Operation()
        {
            CorrelationId = Guid.NewGuid();
            Source = new TaskCompletionSource<OperationResult>();
        }
    }
}
