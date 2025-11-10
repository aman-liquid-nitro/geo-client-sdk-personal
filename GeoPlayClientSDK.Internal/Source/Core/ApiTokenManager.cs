using GeoPlayClientSDK.Internal.Common;
using GeoPlayClientSDK.Internal.Infrastructure.Cache;
using GeoPlayClientSDK.Infrastructure.Network;
using GeoPlayClientSDK.Internal.Models;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GeoPlayClientSDK.Internal.Core
{
    /// <summary>
    /// Manages API verification tokens. This is NOT AppCheck - it's a simple
    /// token system to verify API calls are coming from legitimate SDK instances.
    /// </summary>
    internal class ApiTokenManager : IDisposable
    {
        private readonly GeoPlayConfig _config;
        private readonly IHttpClient _httpClient;
        private readonly ICache _cache;
        private readonly ILogger _logger;
        private Timer _refreshTimer;

        private string _currentToken;
        private bool _isAutoRefreshToken;
        private DateTime _tokenExpiry;
        private readonly SemaphoreSlim _refreshLock = new SemaphoreSlim(1, 1);

        private const string CacheKey = "api_verification_token";
        private const string authEndPoint = "/identity/token";
        private const string serviceSubdomain = "identity";

        public ApiTokenManager(GeoPlayConfig config, IHttpClient baseHttpClient, ICache cache, ILogger logger, bool isAutoRefresh = true)
        {
            _config = config;
            _httpClient = baseHttpClient;
            _cache = cache;
            _logger = logger;
            _isAutoRefreshToken = isAutoRefresh;
        }

        /// <summary>
        /// Initialize by getting the first token from server.
        /// Called during SDK initialization.
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                _logger.Info("[ApiToken] Initializing API verification token...");

                // Try to load from cache first
                var cachedToken = await LoadCachedTokenAsync();
                if (cachedToken != null && !IsTokenExpired(cachedToken))
                {
                    _currentToken = cachedToken;
                    _tokenExpiry = GetTokenExpiry(cachedToken);
                    ScheduleTokenRefresh();
                    _logger.Info("[ApiToken] Using cached verification token");
                    return;
                }

                // No valid cached token, fetch new one
                await RefreshTokenAsync();
            }
            catch (Exception ex)
            {
                _logger.Error($"[ApiToken] Failed to initialize: {ex.Message}");
                throw new GeoPlaySdkException("Failed to initialize API verification token", ex);
            }
        }

        /// <summary>
        /// Gets the current API verification token. Auto-refreshes if expired.
        /// This is called by the HTTP client for EVERY request.
        /// </summary>
        public async Task<string> GetTokenAsync()
        {
            // If token is valid, return it immediately
            if (!string.IsNullOrEmpty(_currentToken) && !IsTokenExpired(_currentToken))
            {
                return _currentToken;
            }

            // Token is expired or missing, refresh it
            await RefreshTokenAsync();
            return _currentToken;
        }

        /// <summary>
        /// Fetches a new verification token from the server.
        /// </summary>
        private async Task RefreshTokenAsync()
        {
            // Prevent multiple simultaneous refresh attempts
            await _refreshLock.WaitAsync();

            try
            {
                // Double-check after acquiring lock
                if (!string.IsNullOrEmpty(_currentToken) && !IsTokenExpired(_currentToken))
                {
                    return;
                }

                _logger.Debug("[ApiToken] Fetching new verification token...");

                // Call your backend endpoint to get verification token
                var response = await _httpClient.PostAsync<ApiTokenResponse>(
                    authEndPoint, serviceSubdomain,
                    body: new ApiTokenRequest(_config.project_id, _config.api_key, "1.0.0", GetPlatform())
                    );

                if (string.IsNullOrEmpty(response.Token))
                {
                    throw new GeoPlaySdkException("Server returned empty verification token");
                }

                _currentToken = response.Token;
                _tokenExpiry = GetTokenExpiry(response.Token);

                // Cache the token
                await _cache.SetAsync(CacheKey, _currentToken,
                    TimeSpan.FromHours(23)); // Cache for 23 hours

                // Schedule auto-refresh
                if (_isAutoRefreshToken)
                    ScheduleTokenRefresh();

                _logger.Info($"[ApiToken] Verification token obtained (expires: {_tokenExpiry:HH:mm:ss})");
            }
            catch (Exception ex)
            {
                _logger.Error($"[ApiToken] Token refresh failed: {ex.Message}");
                throw new GeoPlaySdkException("Failed to refresh API verification token", ex);
            }
            finally
            {
                _refreshLock.Release();
            }
        }

        private async Task<string> LoadCachedTokenAsync()
        {
            try
            {
                return await _cache.GetAsync<string>(CacheKey);
            }
            catch
            {
                return null;
            }
        }

        private bool IsTokenExpired(string token)
        {
            try
            {
                var expiry = GetTokenExpiry(token);
                // Add 1 minute buffer
                return expiry <= DateTime.UtcNow.AddMinutes(1);
            }
            catch
            {
                return true; // If we can't parse it, consider it expired
            }
        }

        private DateTime GetTokenExpiry(string token)
        {
            try
            {
                // Manual JWT parsing
                var parts = token.Split('.');
                if (parts.Length != 3)
                    return DateTime.MinValue;

                // Decode the payload (middle part)
                var payload = parts[1];

                // Add padding if needed
                payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');

                var payloadBytes = Convert.FromBase64String(payload);
                var payloadJson = Encoding.UTF8.GetString(payloadBytes);
                
                var tokenData = JsonConvert.DeserializeObject<JwtPayload>(payloadJson);
                if (tokenData != null && tokenData.exp > 0)
                {
                    return DateTimeOffset.FromUnixTimeSeconds(tokenData.exp).UtcDateTime;
                }

                return DateTime.MinValue;
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        private void ScheduleTokenRefresh()
        {
            _refreshTimer?.Dispose();

            // Refresh 5 minutes before expiry
            var refreshTime = _tokenExpiry.AddMinutes(-5);
            var delay = refreshTime - DateTime.UtcNow;

            if (delay > TimeSpan.Zero)
            {
                _logger.Debug($"[ApiToken] Auto-refresh scheduled in {delay.TotalMinutes:F1} minutes");

                _refreshTimer = new Timer(async _ =>
                {
                    try
                    {
                        await RefreshTokenAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"[ApiToken] Auto-refresh failed: {ex.Message}");
                    }
                }, null, delay, Timeout.InfiniteTimeSpan);
            }
        }

        private string GetPlatform()
        {
#if UNITY_IOS
            return "ios";
#elif UNITY_ANDROID
            return "android";
#elif UNITY_STANDALONE_WIN
            return "windows";
#elif UNITY_STANDALONE_OSX
            return "macos";
#elif UNITY_WEBGL
            return "webgl";
#else
            return "unknown";
#endif
        }

        public void Dispose()
        {
            _refreshTimer?.Dispose();
            _refreshLock?.Dispose();
        }

        private class ApiTokenRequest
        {
            public string projectId;
            public string apiKey;
            //public string appId;
            public string sdkVersion;
            public string platform;

            public ApiTokenRequest(string projectId, string apiKey, string sdkVersion, string platform)
            {
                this.projectId = projectId;
                this.apiKey = apiKey;
                this.sdkVersion = sdkVersion;
                this.platform = platform;
            }
        }

        private class ApiTokenResponse
        {
            public string Token { get; set; }
        }

        [Serializable]
        private class JwtPayload
        {
            public long exp; // Expiration timestamp
            // Add other claims if needed:
            // public string iss;
            // public string aud;
            // public long iat;
        }
    }
}