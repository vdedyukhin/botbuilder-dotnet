﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.Bot.StreamingExtensions
{
    /// <summary>
    /// Implemented by classes used to process incoming requests sent over an <see cref="IStreamingTransport"/> and adhering to the Bot Framework Protocol v3 with Streaming Extensions.
    /// </summary>
    public abstract class RequestHandler
    {
        /// <summary>
        /// The method that must be implemented in order to handle incoming requests.
        /// </summary>
        /// <param name="request">A <see cref="ReceiveRequest"/> for this handler to process.</param>
        /// <param name="context">Optional context to process the request within.</param>
        /// <param name="logger">Optional logger.</param>
        /// <returns>A <see cref="Task"/> that will produce a <see cref="Response"/> on successful completion.</returns>
        public abstract Task<Response> ProcessRequestAsync(ReceiveRequest request, object context = null, ILogger<RequestHandler> logger = null);
    }
}