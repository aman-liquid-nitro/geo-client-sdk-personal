using GeoPlayClientSDK.Internal.Common;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GeoPlayClientSDK.Infrastructure.Network
{
    public class HttpClientWrapper : IHttpClient, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly ILogger _logger;

        public HttpClientWrapper(string baseUrl, ILogger logger, TimeSpan? timeout = null)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _logger = logger;
            _httpClient = new HttpClient
            {
                Timeout = timeout ?? TimeSpan.FromSeconds(30)
            };
        }

        public async Task<TResponse> GetAsync<TResponse>(string endpoint, string service = "",
            Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
        {
            return await SendRequestAsync<TResponse>(HttpMethod.Get, endpoint, service, null, headers, cancellationToken);
        }

        public async Task<TResponse> PostAsync<TResponse>(string endpoint, string service, object body,
            Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
        {
            return await SendRequestAsync<TResponse>(HttpMethod.Post, endpoint, service, body, headers, cancellationToken);
        }

        public async Task<TResponse> PutAsync<TResponse>(string endpoint, string service, object body,
            Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
        {
            return await SendRequestAsync<TResponse>(HttpMethod.Put, endpoint, service, body, headers, cancellationToken);
        }

        private async Task<TResponse> SendRequestAsync<TResponse>(HttpMethod method, string endpoint, string service,
            object? body, Dictionary<string, string>? headers, CancellationToken cancellationToken)
        {
            var finalBaseUrl = string.IsNullOrEmpty(service) ? _baseUrl : string.Format(_baseUrl, service);
            var url = $"{finalBaseUrl}/{endpoint.TrimStart('/')}";
            _logger.Debug($"{method} {url}");

            using var request = new HttpRequestMessage(method, url);

            // Add headers
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            // Add body for POST/PUT
            if (body != null && (method == HttpMethod.Post || method == HttpMethod.Put))
            {

                var json = JsonConvert.SerializeObject(body);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            try
            {
                var response = await _httpClient.SendAsync(request, cancellationToken);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.Error($"HTTP {(int)response.StatusCode}: {content}");
                    throw new NetworkException($"Request failed: {response.StatusCode}", (int)response.StatusCode);
                }

                _logger.Debug($"Response: {content.Substring(0, Math.Min(200, content.Length))}...");

                return JsonConvert.DeserializeObject<TResponse>(content) ?? throw new InvalidOperationException("Failed to deserialize response");
            }
            catch (HttpRequestException ex)
            {
                _logger.Error($"Network error: {ex.Message}");
                throw new NetworkException("Network request failed", 0);
            }
            catch (TaskCanceledException)
            {
                _logger.Error("Request timeout");
                throw new NetworkException("Request timeout", 408);
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}