using System;
using System.Threading;
using System.Threading.Tasks;
using GeoPlayClientSDK.Internal.Core.Dependency;
using GeoPlayClientSDK.Internal.Core;

namespace GeoPlayClientSDK.Internal.Dependency
{
    internal class ConfigFileDependency : IDependencyCheck
    {
        public string Name => "ConfigFile";

        public Task<DependencyCheckResult> CheckAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var exists = ConfigLoader.ConfigFileExists();
                if (!exists)
                {
                    return Task.FromResult(DependencyCheckResult.Unavailable(Name, DependencyStatus.UnavailableMissing, "geoplay-config.json not found"));
                }

                return Task.FromResult(DependencyCheckResult.Available(Name));
            }
            catch (Exception ex)
            {
                return Task.FromResult(DependencyCheckResult.Unavailable(Name, DependencyStatus.UnavailableOther, ex.Message, ex));
            }
        }
    }
}