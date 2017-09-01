using EventStore.Client.Operations;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace EventStore.Client.Internal
{
    internal class SimpleQueuedHandler
    {
        private readonly ConcurrentQueue<Operation> _operationsQueue = new ConcurrentQueue<Operation>();
        private readonly Dictionary<Type, Action<Operation>> _handlers = new Dictionary<Type, Action<Operation>>();
        private int _isProcessing;

        public void RegisterHandler<T>(Action<T> handler) where T : Operation
        {
            _handlers.Add(typeof(T), msg => handler((T)msg));
        }

        public void EnqueueOperation(Operation operation)
        {
            _operationsQueue.Enqueue(operation);
            if (Interlocked.CompareExchange(ref _isProcessing, 1, 0) == 0)
                ThreadPool.QueueUserWorkItem(ProcessQueue);
        }

        private void ProcessQueue(object state)
        {
            do
            {
                Operation operation;

                while (_operationsQueue.TryDequeue(out operation))
                {
                    Action<Operation> handler;
                    if (!_handlers.TryGetValue(operation.GetType(), out handler))
                        throw new Exception(string.Format("No handler registered for operation {0}", operation.GetType().Name));
                    handler(operation);
                }

                Interlocked.Exchange(ref _isProcessing, 0);
            } while (_operationsQueue.Count > 0 && Interlocked.CompareExchange(ref _isProcessing, 1, 0) == 0);
        }
    }
}
