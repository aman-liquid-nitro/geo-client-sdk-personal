using GeoPlayClientSDK.Internal.Core;
using GeoPlayClientSDK.Internal.Core.Dependency;
using System;
using System.Threading.Tasks;

namespace GeoPlayClientSDK.Public
{
    /// <summary>
    /// Main entry point for the SDK. Initialize before using any services.
    /// </summary>
    public sealed class GeoPlayApp
    {
        private static GeoPlayApp _defaultInstance;
        private static readonly object _lock = new object();

        private GeoPlayApp() { }

        /// <summary>
        /// Gets the default SDK instance. Only available after CheckAndFixDependenciesAsync completes.
        /// </summary>
        public static GeoPlayApp DefaultInstance
        {
            get
            {
                if (_defaultInstance == null)
                {
                    throw new InvalidOperationException(
                        "SDK not initialized. Call CheckAndFixDependenciesAsync first.");
                }
                return _defaultInstance;
            }
        }

        /// <summary>
        /// Creates and initializes the SDK with all dependencies.
        /// </summary>
        public static Task<DependencyStatus> CreateApp()
        {
            return Task.Run(async () =>
            {
                try
                {
                    lock (_lock)
                    {
                        if (_defaultInstance != null)
                        {
                            return DependencyStatus.Available;
                        }
                    }

                    // Delegate all initialization logic to the core
                    var status = await GeoPlayCore.CreateAppInternal();

                    if (status == DependencyStatus.Available)
                    {
                        lock (_lock)
                        {
                            _defaultInstance = new GeoPlayApp();
                        }
                    }

                    return status;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SDK] Initialization failed: {ex.Message}");
                    return DependencyStatus.UnavailableOther;
                }
            });
        }

        /// <summary>
        /// Disposes the SDK and releases all resources.
        /// </summary>
        public static void Dispose()
        {
            GeoPlayCore.Instance?.Dispose();
            lock (_lock)
            {
                _defaultInstance = null;
            }
        }
    }
}
