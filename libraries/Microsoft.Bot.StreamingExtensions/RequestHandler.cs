﻿using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.Bot.StreamingExtensions
{
    public abstract class RequestHandler
    {
        public abstract Task<Response> ProcessRequestAsync(ReceiveRequest request, object context = null, ILogger<RequestHandler> logger = null);
    }
}