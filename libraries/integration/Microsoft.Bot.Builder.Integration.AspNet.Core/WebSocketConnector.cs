﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.StreamingExtensions.Transport;
using Microsoft.Bot.StreamingExtensions.Transport.WebSockets;
using Microsoft.Extensions.Logging;

namespace Microsoft.Bot.Builder.Integration.AspNet.Core.StreamingExtensions
{
    internal class WebSocketConnector
    {
        // These headers are used to send the required values for validation of an incoming connection request from an ABS channel.
        // TODO: We must document this somewhere, right? Find it and put a reference link here.
        private const string AuthHeaderName = "authorization";
        private const string ChannelIdHeaderName = "channelid";
        private readonly IChannelProvider _channelProvider;
        private readonly ICredentialProvider _credentialProvider;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketConnector"/> class.
        /// Constructor for use when establishing a connection with a WebSocket server.
        /// </summary>
        /// <param name="credentialProvider">Used for validating channel credential authentication information.</param>
        /// <param name="channelProvider">Used for validating channel authentication information.</param>
        /// <param name="logger">Set in order to enable logging.</param>
        internal WebSocketConnector(ICredentialProvider credentialProvider, IChannelProvider channelProvider = null, ILogger logger = null)
        {
            _credentialProvider = credentialProvider;
            _channelProvider = channelProvider;
            _logger = logger;
        }

        /// <summary>
        /// Process the initial request to establish a long lived connection via a streaming server.
        /// </summary>
        /// <param name="onTurnError"> The function to execute on turn errors.</param>
        /// <param name="middlewareSet">The set of middleware to perform on each turn.</param>
        /// <param name="httpRequest">The connection request.</param>
        /// <param name="httpResponse">The response sent on error or connection termination.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Returns on task completion.</returns>
        internal async Task ProcessAsync(Func<ITurnContext, Exception, Task> onTurnError, List<Builder.IMiddleware> middlewareSet, HttpRequest httpRequest, HttpResponse httpResponse, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (httpRequest == null)
            {
                var ex = new ArgumentNullException(nameof(httpRequest));
                _logger?.LogError(ex.Message);

                throw ex;
            }

            if (httpResponse == null)
            {
                var ex = new ArgumentNullException(nameof(httpResponse));
                _logger?.LogError(ex.Message);

                throw ex;
            }

            if (!httpRequest.HttpContext.WebSockets.IsWebSocketRequest)
            {
                httpRequest.HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                await httpRequest.HttpContext.Response.WriteAsync("Upgrade to WebSocket is required.").ConfigureAwait(false);
                _logger?.LogInformation("Invalid request: Request was not a WebSocket handshake.");

                return;
            }

            try
            {
                if (!await _credentialProvider.IsAuthenticationDisabledAsync().ConfigureAwait(false))
                {
                    var authHeader = httpRequest.Headers.Where(x => x.Key.ToLower() == AuthHeaderName).FirstOrDefault().Value.FirstOrDefault();
                    var channelId = httpRequest.Headers.Where(x => x.Key.ToLower() == ChannelIdHeaderName).FirstOrDefault().Value.FirstOrDefault();

                    if (string.IsNullOrWhiteSpace(authHeader))
                    {
                        await MissingAuthHeaderHelperAsync(AuthHeaderName, httpRequest).ConfigureAwait(false);

                        return;
                    }

                    if (string.IsNullOrWhiteSpace(channelId))
                    {
                        await MissingAuthHeaderHelperAsync(ChannelIdHeaderName, httpRequest).ConfigureAwait(false);

                        return;
                    }

                    var claimsIdentity = await JwtTokenValidation.ValidateAuthHeader(authHeader, _credentialProvider, _channelProvider, channelId).ConfigureAwait(false);
                    if (!claimsIdentity.IsAuthenticated)
                    {
                        httpRequest.HttpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                        _logger?.LogInformation("Unauthorized connection attempt.");

                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                httpRequest.HttpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                await httpRequest.HttpContext.Response.WriteAsync("Error while attempting to authorize connection.").ConfigureAwait(false);
                _logger?.LogError(ex.Message);

                return;
            }

            await CreateStreamingServerConnectionAsync(onTurnError, middlewareSet, httpRequest.HttpContext).ConfigureAwait(false);
        }

        private async Task MissingAuthHeaderHelperAsync(string headerName, HttpRequest httpRequest)
        {
            httpRequest.HttpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            _logger?.LogInformation($"Unable to authentiate. Missing header: {headerName}");
            await httpRequest.HttpContext.Response.WriteAsync($"Unable to authentiate. Missing header: {headerName}").ConfigureAwait(false);
        }

        private async Task CreateStreamingServerConnectionAsync(Func<ITurnContext, Exception, Task> onTurnError, List<Builder.IMiddleware> middlewareSet, HttpContext httpContext)
        {
            var handler = new StreamingRequestHandler(onTurnError, httpContext.RequestServices, middlewareSet);
            _logger?.LogInformation("Creating server for WebSocket connection.");
            var socket = await httpContext.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);
            IStreamingTransportServer server = new WebSocketServer(socket, handler);

            if (server == null)
            {
                httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                await httpContext.Response.WriteAsync("Unable to create transport server.").ConfigureAwait(false);
                _logger?.LogInformation("Failed to create server.");

                return;
            }

            handler.Server = server;
            await server.StartAsync().ConfigureAwait(false);
        }
    }
}