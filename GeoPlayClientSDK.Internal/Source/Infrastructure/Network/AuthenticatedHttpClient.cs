using GeoPlayClientSDK.Internal.Common;
using GeoPlayClientSDK.Infrastructure.Network;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GeoPlayClientSDK.Infrastructure
{
    internal class AuthenticatedHttpClient
    {
        private readonly IHttpClient _innerClient;
        private readonly Func<Task<string>> _tokenProvider;
        private readonly ILogger _logger;

        public AuthenticatedHttpClient(IHttpClient innerClient, Func<Task<string>> apiTokenProvider, ILogger logger)
        {
            _innerClient = innerClient;
            _tokenProvider = apiTokenProvider;
            _logger = logger;
        }

        public async Task<TResponse> GetAsync<TResponse>(string endpoint, string service)
        {
            var headers = await BuildAuthHeaderAsync();
            return await _innerClient.GetAsync<TResponse>(endpoint, service, headers);
        }

        public async Task<TResponse> PostAsync<TResponse>(string endpoint, string service, object body)
        {
            var headers = await BuildAuthHeaderAsync();
            return await _innerClient.PostAsync<TResponse>(endpoint, service, body, headers);
        }

        public async Task<TResponse> PutAsync<TResponse>(string endpoint, string servive, object body)
        {
            var headers = await BuildAuthHeaderAsync();
            return await _innerClient.PutAsync<TResponse>(endpoint, servive, body, headers);
        }

        private async Task<Dictionary<string, string>> BuildAuthHeaderAsync()
        {
            var headers = new Dictionary<string, string>();

            try
            {
                var apiToken = await _tokenProvider();
                if (!string.IsNullOrEmpty(apiToken))
                {
                    headers["X-API-Token"] = apiToken;
                    _logger.Debug("[HTTP] API verification token added");
                }
                else
                {
                    _logger.Error("[HTTP] API verification token is missing!");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"[HTTP] Failed to get API token: {ex.Message}");
                throw new GeoPlaySdkException("Cannot make API call without verification token", ex);
            }

            return headers;
        }
    }
}
