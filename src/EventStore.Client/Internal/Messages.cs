using EventStore.Client.Messages;
using EventStore.Client.SystemData;
using EventStore.Client.Transport.Tcp;
using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace EventStore.Client.Internal
{
    internal abstract class Message
    {
        public Guid CorrelationId;
        public TaskCompletionSource<Result> Source;
        public Task<Result> Task => Source.Task;
        public Message()
        {
            CorrelationId = Guid.NewGuid();
            Source = new TaskCompletionSource<Result>();
        }
    }

    internal class TimerTickMessage : Message
    {
    }

    internal class StartConnectionMessage : Message
    {
        public readonly Connect _operation;
        //public readonly IEndPointDiscoverer EndPointDiscoverer;

        public StartConnectionMessage(Connect operation)//, IEndPointDiscoverer endPointDiscoverer)
        {
            //Ensure.NotNull(task, "task");
            //Ensure.NotNull(endPointDiscoverer, "endendPointDiscoverer");

            _operation = operation;
            //EndPointDiscoverer = endPointDiscoverer;
        }
    }

    internal class CloseConnectionMessage : Message
    {
        public readonly string Reason;
        public readonly Exception Exception;

        public CloseConnectionMessage(string reason, Exception exception)
        {
            Reason = reason;
            Exception = exception;
        }
    }

    internal class EstablishTcpConnectionMessage : Message
    {
        //public readonly NodeEndPoints EndPoints;

        //public EstablishTcpConnectionMessage(NodeEndPoints endPoints)
        //{
        //    EndPoints = endPoints;
        //}
    }

    internal class TcpConnectionEstablishedMessage : Message
    {
        public readonly TcpPackageConnection Connection;

        public TcpConnectionEstablishedMessage(TcpPackageConnection connection)
        {
            //Ensure.NotNull(connection, "connection");
            Connection = connection;
        }
    }

    internal class TcpConnectionClosedMessage : Message
    {
        public readonly TcpPackageConnection Connection;
        public readonly SocketError Error;

        public TcpConnectionClosedMessage(TcpPackageConnection connection, SocketError error)
        {
            //Ensure.NotNull(connection, "connection");
            Connection = connection;
            Error = error;
        }
    }

    internal class StartOperationMessage : Message
    {
        //public readonly IClientOperation Operation;
        //public readonly int MaxRetries;
        //public readonly TimeSpan Timeout;

        //public StartOperationMessage(IClientOperation operation, int maxRetries, TimeSpan timeout)
        //{
        //    Ensure.NotNull(operation, "operation");
        //    Operation = operation;
        //    MaxRetries = maxRetries;
        //    Timeout = timeout;
        //}
    }

    internal class HandleTcpPackageMessage : Message
    {
        public readonly TcpPackageConnection Connection;
        public readonly TcpPackage Package;

        public HandleTcpPackageMessage(TcpPackageConnection connection, TcpPackage package)
        {
            Connection = connection;
            Package = package;
        }
    }

    internal class TcpConnectionErrorMessage : Message
    {
        public readonly TcpPackageConnection Connection;
        public readonly Exception Exception;

        public TcpConnectionErrorMessage(TcpPackageConnection connection, Exception exception)
        {
            Connection = connection;
            Exception = exception;
        }
    }
}
