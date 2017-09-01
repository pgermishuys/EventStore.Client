using EventStore.Client.Common.Log;
using EventStore.Client.Internal;
using EventStore.Client.Operations;
using System;
using System.Net;
using System.Threading.Tasks;

namespace EventStore.Client
{
    public class EventStoreConnection : IEventStoreConnection
    {
        private EventStoreConnectionSettings _connectionSettings;
        private IPEndPoint _tcpEndpoint;
        private SimpleQueuedHandler _queue;
        private TcpPackageConnection _connection;
        private ConnectionState _state = ConnectionState.Init;
        private ConnectingPhase _connectingPhase = ConnectingPhase.Invalid;

        public EventStoreConnection(IPEndPoint tcpEndpoint, EventStoreConnectionSettings settings)
        {
            _queue = new SimpleQueuedHandler();
            _tcpEndpoint = tcpEndpoint;
            _connectionSettings = settings;

            _queue.RegisterHandler<ConnectOperation>(operation => EstablishTcpConnection(operation));
        }

        public async Task<OperationResult> Connect()
        {
            var operation = new ConnectOperation();
            _queue.EnqueueOperation(operation);
            return await operation.Task;
        }

        private void EstablishTcpConnection(ConnectOperation operation)
        {
            if (_tcpEndpoint == null)
            {
                //CloseConnection("No end point to node specified.");
                return;
            }

            //Log.Debug("EstablishTcpConnection to [{0}]", _tcpEndpoint);

            if (_state != ConnectionState.Connecting) return;
            if (_connectingPhase != ConnectingPhase.EndPointDiscovery) return;

            _connectingPhase = ConnectingPhase.ConnectionEstablishing;
            _connection = new TcpPackageConnection(
                    _connectionSettings.Logger,
                    _tcpEndpoint,
                    Guid.NewGuid(),
                    false, //_settings.UseSslConnection,
                    String.Empty, //_settings.TargetHost,
                    false, //_settings.ValidateServer,
                    TimeSpan.FromSeconds(10), //_settings.ClientConnectionTimeout,
                    (connection, package) => { },//_queue.EnqueueOperation(new HandleTcpPackageMessage(connection, package)),
                    (connection, exc) => { },//_queue.EnqueueOperation(new TcpConnectionErrorMessage(connection, exc)),
                    connection => { },//_queue.EnqueueOperation(new TcpConnectionEstablishedMessage(connection)),
                    (connection, error) => { }//_queue.EnqueueOperation(new TcpConnectionClosedMessage(connection, error)));
            );
            _connection.StartReceiving();
            operation.Source.SetResult(new Success());
        }

        private enum ConnectionState
        {
            Init,
            Connecting,
            Connected,
            Closed
        }

        private enum ConnectingPhase
        {
            Invalid,
            Reconnecting,
            EndPointDiscovery,
            ConnectionEstablishing,
            Authentication,
            Identification,
            Connected
        }
    }
}