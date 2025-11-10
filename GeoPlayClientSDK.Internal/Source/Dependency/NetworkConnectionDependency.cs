using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using GeoPlayClientSDK.Internal.Core.Dependency;

namespace GeoPlayClientSDK.Internal.Core
{
    internal class NetworkConnectionDependency : IDependencyCheck
    {
        public string Name => "Network";

        // DNS lookup timeout used to determine internet reachability.
        private readonly TimeSpan _dnsTimeout;

        public NetworkConnectionDependency(TimeSpan? dnsTimeout = null)
        {
            _dnsTimeout = dnsTimeout ?? TimeSpan.FromSeconds(5);
        }

        public async Task<DependencyCheckResult> CheckAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Quick local network availability check
                if (!NetworkInterface.GetIsNetworkAvailable())
                {
                    return DependencyCheckResult.Unavailable(
                        Name,
                        DependencyStatus.UnavailableOther,
                        "No network interfaces are available");
                }

                // Try a lightweight DNS resolution to check actual internet reachability.
                // We avoid hard HTTP calls so the check is fast and has few side effects.
                var lookupTask = Dns.GetHostEntryAsync("google.com");
                var timeoutTask = Task.Delay(_dnsTimeout, cancellationToken);

                var completed = await Task.WhenAny(lookupTask, timeoutTask).ConfigureAwait(false);

                if (completed == timeoutTask)
                {
                    if (cancellationToken.IsCancellationRequested)
                        return DependencyCheckResult.Unavailable(Name, DependencyStatus.UnavailableOther, "Network check canceled");

                    return DependencyCheckResult.Unavailable(
                        Name,
                        DependencyStatus.UnavailableOther,
                        $"DNS resolution timed out after {_dnsTimeout.TotalSeconds:F1}s");
                }

                // Ensure DNS lookup succeeded and returned at least one address
                var entry = await lookupTask.ConfigureAwait(false);
                if (entry?.AddressList?.Length > 0)
                {
                    return DependencyCheckResult.Available(Name);
                }

                return DependencyCheckResult.Unavailable(
                    Name,
                    DependencyStatus.UnavailableOther,
                    "DNS lookup returned no addresses");
            }
            catch (OperationCanceledException)
            {
                return DependencyCheckResult.Unavailable(Name, DependencyStatus.UnavailableOther, "Network check canceled");
            }
            catch (Exception ex)
            {
                return DependencyCheckResult.Unavailable(Name, DependencyStatus.UnavailableOther, ex.Message, ex);
            }
        }
    }
}