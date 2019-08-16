// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal static class HttpClientUtility
    {
        private static readonly HttpClient s_httpClient = new HttpClient(new HttpClientHandler()
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
        });

        public static async Task<HttpResponseMessage> GetAsync(string url, Config config, EntityTagHeaderValue etag = null)
        {
            return await RetryUtility.Retry(
                () =>
                {
                    // Create new instance of HttpRequestMessage to avoid System.InvalidOperationException:
                    // "The request message was already sent. Cannot send the same request message multiple times."
                    using (var message = new HttpRequestMessage(HttpMethod.Get, url))
                    {
                        AddAuthorizationHeader(url, message, config);

                        if (etag != null)
                        {
                            message.Headers.IfNoneMatch.Add(etag);
                        }
                        return s_httpClient.SendAsync(message);
                    }
                },
                NeedRetry);

            bool NeedRetry(Exception ex)
            {
                return ex is HttpRequestException
                    || ex is TimeoutException
                    || ex is OperationCanceledException;
            }
        }

        private static void AddAuthorizationHeader(string url, HttpRequestMessage message, Config config)
        {
            foreach (var (baseUrl, rule) in config.Http)
            {
                if (url.StartsWith(baseUrl))
                {
                    foreach (var header in rule.Headers)
                    {
                        message.Headers.Add(header.Key, header.Value);
                    }
                    break;
                }
            }
        }
    }
}