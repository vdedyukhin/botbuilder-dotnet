﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;

namespace Microsoft.Bot.StreamingExtensions
{
    /// <summary>
    /// A set of commonly used response types and helper methods.
    /// </summary>
    public partial class Response
    {
        /// <summary>
        /// Creates a response indicating the requested resource was not found.
        /// </summary>
        /// <param name="body">An optional body containing additional information.</param>
        /// <returns>A response with the appropriate statuscode and passed in body.</returns>
        public static Response NotFound(HttpContent body = null) => CreateResponse(HttpStatusCode.NotFound, body);

        /// <summary>
        /// Creates a response indicating the requested resource is forbidden.
        /// </summary>
        /// <param name="body">An optional body containing additional information.</param>
        /// <returns>A response with the appropriate statuscode and passed in body.</returns>
        public static Response Forbidden(HttpContent body = null) => CreateResponse(HttpStatusCode.Forbidden, body);

        /// <summary>
        /// Creates a response indicating the request was successful.
        /// </summary>
        /// <param name="body">An optional body containing additional information.</param>
        /// <returns>A response with the appropriate statuscode and passed in body.</returns>
        public static Response OK(HttpContent body = null) => CreateResponse(HttpStatusCode.OK, body);

        /// <summary>
        /// Creates a response indicating the server encountered an error while processing the request.
        /// </summary>
        /// <param name="body">An optional body containing additional information.</param>
        /// <returns>A response with the appropriate statuscode and passed in body.</returns>
        public static Response InternalServerError(HttpContent body = null) => CreateResponse(HttpStatusCode.InternalServerError, body);

        /// <summary>
        /// Creates a response using the passed in statusCode and optional body.
        /// </summary>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> to set on the <see cref="Response"/>.</param>
        /// <param name="body">An optional body containing additional information.</param>
        /// <returns>A response with the appropriate statuscode and passed in body.</returns>
        public static Response CreateResponse(HttpStatusCode statusCode, HttpContent body = null)
        {
            var response = new Response()
            {
                StatusCode = (int)statusCode,
            };

            if (body != null)
            {
                response.AddStream(body);
            }

            return response;
        }

        /// <summary>
        /// Adds a new stream to the passed in <see cref="Response"/> containing the passed in content.
        /// Throws <see cref="ArgumentNullException"/> if content is null.
        /// </summary>
        /// <param name="content">An <see cref="HttpContent"/> instance containing the data to insert into the stream.</param>
        public void AddStream(HttpContent content)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            if (Streams == null)
            {
                Streams = new List<HttpContentStream>();
            }

            Streams.Add(
                new HttpContentStream()
                {
                    Content = content,
                });
        }
    }
}