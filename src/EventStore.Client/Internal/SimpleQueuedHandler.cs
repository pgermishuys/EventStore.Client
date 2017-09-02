using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace EventStore.Client.Internal
{
    internal class SimpleQueuedHandler
    {
        private readonly ConcurrentQueue<Message> _operationsQueue = new ConcurrentQueue<Message>();
        private readonly Dictionary<Type, Action<Message>> _handlers = new Dictionary<Type, Action<Message>>();
        private int _isProcessing;

        public void RegisterHandler<T>(Action<T> handler) where T : Message
        {
            _handlers.Add(typeof(T), msg => handler((T)msg));
        }

        public void Enqueue(Message operation)
        {
            _operationsQueue.Enqueue(operation);
            if (Interlocked.CompareExchange(ref _isProcessing, 1, 0) == 0)
                ThreadPool.QueueUserWorkItem(ProcessQueue);
        }

        private void ProcessQueue(object state)
        {
            do
            {
                Message operation;

                while (_operationsQueue.TryDequeue(out operation))
                {
                    Action<Message> handler;
                    if (!_handlers.TryGetValue(operation.GetType(), out handler))
                        throw new Exception(string.Format("No handler registered for operation {0}", operation.GetType().Name));
                    handler(operation);
                }

                Interlocked.Exchange(ref _isProcessing, 0);
            } while (_operationsQueue.Count > 0 && Interlocked.CompareExchange(ref _isProcessing, 1, 0) == 0);
        }
    }
}
