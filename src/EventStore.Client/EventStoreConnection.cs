using EventStore.Client.Transport.Tcp;
using EventStore.Client.Internal;
using EventStore.Client.Messages;
using EventStore.Client.SystemData;
using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using System.Threading;

namespace EventStore.Client
{
    public class EventStoreConnection : IEventStoreConnection
    {
        private readonly byte ClientVersion = 1;
        private EventStoreConnectionSettings _connectionSettings;
        private IPEndPoint _tcpEndpoint;
        private SimpleQueuedHandler _queue;
        private TcpPackageConnection _connection;
        private ConnectionState _state = ConnectionState.Init;
        private ConnectingPhase _connectingPhase = ConnectingPhase.Invalid;

        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private ReconnectionInfo _reconnectionInfo;
        private HeartbeatInfo _heartbeatInfo;
        private AuthInfo _authInfo;
        private IdentifyInfo _identifyInfo;
        private int _packageNumber;
        private int _wasConnected;

        public EventStoreConnection(IPEndPoint tcpEndpoint, EventStoreConnectionSettings settings)
        {
            _queue = new SimpleQueuedHandler();
            _tcpEndpoint = tcpEndpoint;
            _connectionSettings = settings;

            _queue.RegisterHandler<Connect>(operation => StartConnection(operation));
            _queue.RegisterHandler<TcpConnectionEstablishedMessage>(msg => TcpConnectionEstablished(msg.Connection));
            _queue.RegisterHandler<HandleTcpPackageMessage>(msg => HandleTcpPackage(msg.Connection, msg.Package));
        }

        public async Task<Result> Connect()
        {
            var operation = new Connect();
            _queue.Enqueue(operation);
            return await operation.Task;
        }

        private void StartConnection(Connect operation)
        {
            switch (_state)
            {
                case ConnectionState.Init:
                    {
                        _state = ConnectionState.Connecting;
                        _connectingPhase = ConnectingPhase.Reconnecting;
                        DiscoverEndPoint(operation);
                        break;
                    }
                case ConnectionState.Connecting:
                case ConnectionState.Connected:
                    {
                        break;
                    }
                case ConnectionState.Closed:
                    break;
                default: throw new Exception(string.Format("Unknown state: {0}", _state));
            }
        }

        private void DiscoverEndPoint(Connect operation)
        {
            if (_state != ConnectionState.Connecting) return;
            if (_connectingPhase != ConnectingPhase.Reconnecting) return;

            _connectingPhase = ConnectingPhase.EndPointDiscovery;
            EstablishTcpConnection(operation);
        }

        private void EstablishTcpConnection(Connect message)
        {
            if (_tcpEndpoint == null)
            {
                return;
            }

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
                    (connection, package) => { _queue.Enqueue(new HandleTcpPackageMessage(connection, package)); },
                    (connection, exc) => { },//_queue.EnqueueOperation(new TcpConnectionErrorMessage(connection, exc)),
                    connection => { },//_queue.EnqueueOperation(new TcpConnectionEstablishedMessage(connection)),
                    (connection, error) => { }//_queue.EnqueueOperation(new TcpConnectionClosedMessage(connection, error)));
            );
            _connection.StartReceiving();
            message.Source.SetResult(new Success());
        }

        private void TcpConnectionEstablished(TcpPackageConnection connection)
        {
            if (_state != ConnectionState.Connecting || _connection != connection || connection.IsClosed)
            {
                return;
            }

            _heartbeatInfo = new HeartbeatInfo(_packageNumber, true, _stopwatch.Elapsed);
            GoToIdentifyState();
        }

        private void HandleTcpPackage(TcpPackageConnection connection, TcpPackage package)
        {
            if (_connection != connection || _state == ConnectionState.Closed || _state == ConnectionState.Init)
            {
                return;
            }

            _packageNumber += 1;

            if (package.Command == TcpCommand.HeartbeatResponseCommand)
                return;
            if (package.Command == TcpCommand.HeartbeatRequestCommand)
            {
                _connection.EnqueueSend(new TcpPackage(TcpCommand.HeartbeatResponseCommand, package.CorrelationId, null));
                return;
            }

            if (package.Command == TcpCommand.Authenticated || package.Command == TcpCommand.NotAuthenticated)
            {
                if (_state == ConnectionState.Connecting
                    && _connectingPhase == ConnectingPhase.Authentication
                    && _authInfo.CorrelationId == package.CorrelationId)
                {
                    GoToIdentifyState();
                    return;
                }
            }

            if (package.Command == TcpCommand.ClientIdentified)
            {
                if (_state == ConnectionState.Connecting
                    && _identifyInfo.CorrelationId == package.CorrelationId)
                {
                    GoToConnectedState();
                    return;
                }
            }

            if (package.Command == TcpCommand.BadRequest && package.CorrelationId == Guid.Empty)
            {
                return;
            }
        }

        private void GoToIdentifyState()
        {
            _connectingPhase = ConnectingPhase.Identification;

            _identifyInfo = new IdentifyInfo(Guid.NewGuid(), _stopwatch.Elapsed);
            var dto = new ClientMessage.IdentifyClient(ClientVersion, "TestConnectionClientName");
            _connection.EnqueueSend(new TcpPackage(TcpCommand.IdentifyClient, _identifyInfo.CorrelationId, dto.Serialize()));
        }

        private void GoToConnectedState()
        {
            _state = ConnectionState.Connected;
            _connectingPhase = ConnectingPhase.Connected;

            Interlocked.CompareExchange(ref _wasConnected, 1, 0);
        }

        private struct HeartbeatInfo
        {
            public readonly int LastPackageNumber;
            public readonly bool IsIntervalStage;
            public readonly TimeSpan TimeStamp;

            public HeartbeatInfo(int lastPackageNumber, bool isIntervalStage, TimeSpan timeStamp)
            {
                LastPackageNumber = lastPackageNumber;
                IsIntervalStage = isIntervalStage;
                TimeStamp = timeStamp;
            }
        }

        private struct ReconnectionInfo
        {
            public readonly int ReconnectionAttempt;
            public readonly TimeSpan TimeStamp;

            public ReconnectionInfo(int reconnectionAttempt, TimeSpan timeStamp)
            {
                ReconnectionAttempt = reconnectionAttempt;
                TimeStamp = timeStamp;
            }
        }

        private struct AuthInfo
        {
            public readonly Guid CorrelationId;
            public readonly TimeSpan TimeStamp;

            public AuthInfo(Guid correlationId, TimeSpan timeStamp)
            {
                CorrelationId = correlationId;
                TimeStamp = timeStamp;
            }
        }

        private struct IdentifyInfo
        {
            public readonly Guid CorrelationId;
            public readonly TimeSpan TimeStamp;

            public IdentifyInfo(Guid correlationId, TimeSpan timeStamp)
            {
                CorrelationId = correlationId;
                TimeStamp = timeStamp;
            }
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