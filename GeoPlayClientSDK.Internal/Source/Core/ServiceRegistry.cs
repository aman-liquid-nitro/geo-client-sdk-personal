using System;
using System.Collections.Generic;

namespace GeoPlayClientSDK.Internal.Core
{
    internal class ServiceRegistry : IDisposable
    {
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();
        private readonly object _lock = new object();
        private bool _disposed;

        public void Register<TService>(TService service) where TService : class
        {
            lock (_lock)
            {
                var serviceType = typeof(TService);
                if (_services.ContainsKey(serviceType))
                {
                    throw new InvalidOperationException($"Service {serviceType.Name} already registered");
                }
                _services[serviceType] = service;
            }
        }

        public TService GetService<TService>() where TService : class
        {
            lock (_lock)
            {
                var serviceType = typeof(TService);
                if (!_services.TryGetValue(serviceType, out var service))
                {
                    throw new InvalidOperationException($"Service {serviceType.Name} not registered");
                }
                return service as TService;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            lock (_lock)
            {
                foreach (var service in _services.Values)
                {
                    if (service is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
                _services.Clear();
            }
            _disposed = true;
        }
    }
}
