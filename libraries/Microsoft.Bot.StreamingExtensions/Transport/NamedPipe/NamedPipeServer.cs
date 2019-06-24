﻿using System;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.StreamingExtensions.Payloads;
using Microsoft.Bot.StreamingExtensions.PayloadTransport;
using Microsoft.Bot.StreamingExtensions.Utilities;

namespace Microsoft.Bot.StreamingExtensions.Transport.NamedPipes
{
    public class NamedPipeServer : IStreamingTransportServer
    {
        private readonly string _baseName;
        private readonly RequestHandler _requestHandler;
        private readonly RequestManager _requestManager;
        private readonly IPayloadSender _sender;
        private readonly IPayloadReceiver _receiver;
        private readonly ProtocolAdapter _protocolAdapter;
        private readonly bool _autoReconnect;
        private object _syncLock = new object();
        private bool _isDisconnecting = false;

        public NamedPipeServer(string baseName, RequestHandler requestHandler, bool autoReconnect = true)
        {
            _baseName = baseName;
            _requestHandler = requestHandler;
            _autoReconnect = autoReconnect;

            _requestManager = new RequestManager();

            _sender = new PayloadSender();
            _sender.Disconnected += OnConnectionDisconnected;
            _receiver = new PayloadReceiver();
            _receiver.Disconnected += OnConnectionDisconnected;

            _protocolAdapter = new ProtocolAdapter(_requestHandler, _requestManager, _sender, _receiver);
        }

        public event DisconnectedEventHandler Disconnected;

        public async Task StartAsync()
        {
            var incomingPipeName = _baseName + NamedPipeTransport.ServerIncomingPath;
            var incomingServer = new NamedPipeServerStream(incomingPipeName, PipeDirection.In, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte, PipeOptions.WriteThrough | PipeOptions.Asynchronous);
            await incomingServer.WaitForConnectionAsync().ConfigureAwait(false);

            var outgoingPipeName = _baseName + NamedPipeTransport.ServerOutgoingPath;
            var outgoingServer = new NamedPipeServerStream(outgoingPipeName, PipeDirection.Out, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte, PipeOptions.WriteThrough | PipeOptions.Asynchronous);
            await outgoingServer.WaitForConnectionAsync().ConfigureAwait(false);

            _sender.Connect(new NamedPipeTransport(outgoingServer));
            _receiver.Connect(new NamedPipeTransport(incomingServer));
        }

        public async Task<ReceiveResponse> SendAsync(Request request, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await _protocolAdapter.SendRequestAsync(request, cancellationToken).ConfigureAwait(false);
        }

        public void Disconnect()
        {
            _sender.Disconnect();
            _receiver.Disconnect();
        }

        private void OnConnectionDisconnected(object sender, EventArgs e)
        {
            bool doDisconnect = false;
            if (!_isDisconnecting)
            {
                lock (_syncLock)
                {
                    if (!_isDisconnecting)
                    {
                        _isDisconnecting = true;
                        doDisconnect = true;
                    }
                }
            }

            if (doDisconnect)
            {
                try
                {
                    if (_sender.IsConnected)
                    {
                        _sender.Disconnect();
                    }

                    if (_receiver.IsConnected)
                    {
                        _receiver.Disconnect();
                    }

                    Disconnected?.Invoke(this, DisconnectedEventArgs.Empty);

                    if (_autoReconnect)
                    {
                        // Try to rerun the server connection
                        Background.Run(StartAsync);
                    }
                }
                finally
                {
                    lock (_syncLock)
                    {
                        _isDisconnecting = false;
                    }
                }
            }
        }
    }
}