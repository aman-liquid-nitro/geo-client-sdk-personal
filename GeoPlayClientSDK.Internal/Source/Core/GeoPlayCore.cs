using GeoPlayClientSDK.Internal.Common;
using GeoPlayClientSDK.Infrastructure;
using GeoPlayClientSDK.Internal.Infrastructure.Cache;
using GeoPlayClientSDK.Infrastructure.Network;
using GeoPlayClientSDK.Internal.Core.Dependency;
using GeoPlayClientSDK.Internal.Models;
using System;
using System.Threading.Tasks;

namespace GeoPlayClientSDK.Internal.Core
{
    public sealed class GeoPlayCore : IDisposable
    {
        private static GeoPlayCore _instance;
        private static readonly object _lock = new object();

        private readonly GeoPlayConfig _config;
        private readonly ServiceRegistry _serviceRegistry;
        private ApiTokenManager _apiTokenManager;
        private bool _isInitialized;
        private bool _disposed;

        private GeoPlayCore(GeoPlayConfig config)
        {
            _config = config;
            _serviceRegistry = new ServiceRegistry();
        }

        //todo :
        // Change to Create method
        // Make this synchronous
        public static async Task<DependencyStatus> CreateAppInternal()
        {
            if (_instance != null && _instance._isInitialized)
            {
                return DependencyStatus.Available;
            }

            // Check dependencies first
            var dependencyChecker = new DependencyChecker();
            var dependencyStatus = await dependencyChecker.CheckDependenciesAsync();

            if (dependencyStatus != DependencyStatus.Available)
            {
                return dependencyStatus;
            }

            // Load configuration
            var configLoader = new ConfigLoader();
            var config = await configLoader.LoadConfigAsync();

            if (config == null)
            {
                return DependencyStatus.UnavailableInvalid;
            }

            lock (_lock)
            {
                if (_instance != null)
                {
                    return _instance._isInitialized ? DependencyStatus.Available : DependencyStatus.UnavailableOther;
                }

                _instance = new GeoPlayCore(config);
            }

            // TODO: Recheck this for later
            // Initialize services
            var initializationSuccess = await _instance.InitializeServicesAsync();
            return initializationSuccess ? DependencyStatus.Available : DependencyStatus.UnavailableOther;
        }

        public static GeoPlayCore Instance
        {
            get
            {
                if (_instance == null)
                {
                    throw new InvalidOperationException("SDKCore not initialized");
                }
                return _instance;
            }
        }

        private async Task<bool> InitializeServicesAsync()
        {
            if (_isInitialized) return true;

            try
            {
                // Register infrastructure
                var logger = new ConsoleLogger();
                var cache = new MemoryCache();

                logger.Info("[SDK] Initializing services...");

                _serviceRegistry.Register<ICache>(cache);
                _serviceRegistry.Register<ILogger>(logger);

                // HTTP client
                var httpClient = new HttpClientWrapper(_config.base_url, logger);
                _serviceRegistry.Register(httpClient);

                _apiTokenManager = new ApiTokenManager(_config, httpClient, cache, logger, isAutoRefresh: true);
                await _apiTokenManager.InitializeAsync(); // Blocks until we have a token
                _serviceRegistry.Register(_apiTokenManager);
                logger.Info("[SDK] API verification token ready");
                
                var authenticatedHttpClient = new AuthenticatedHttpClient(httpClient,
                    apiTokenProvider: async () => await _apiTokenManager.GetTokenAsync(),
                    logger);
                _serviceRegistry.Register(authenticatedHttpClient);

                _isInitialized = true;
                logger.Info("[SDK] All services initialized successfully");
                return true;
            }
            catch (Exception ex)
            {
                new GeoPlaySdkException($"[SDK] Service initialization failed", ex);
                _isInitialized = false;
                return false;
            }
        }

        public bool IsInitialized => _isInitialized;

        public TService GetService<TService>() where TService : class
        {
            return _serviceRegistry.GetService<TService>();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _serviceRegistry?.Dispose();
            _disposed = true;
            lock (_lock)
            {
                _instance = null;
            }
        }
    }
}
