﻿using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Bot.StreamingExtensions
{
    public static class StreamExtensions
    {
        /// <summary>
        /// Read the contents of the Concurrent stream and convert to an Utf8 string.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static async Task<string> ReadAsUtf8StringAsync(this Stream stream)
        {
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                return await reader.ReadToEndAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Read the contents of the Concurrent stream and convert to an Utf8 string.
        /// </summary>
        /// <returns>Stream contents as string.</returns>
        public static string ReadAsUtf8String(this Stream stream)
        {
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }
    }
}