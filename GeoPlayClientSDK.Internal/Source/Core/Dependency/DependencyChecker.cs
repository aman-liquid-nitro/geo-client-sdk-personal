using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GeoPlayClientSDK.Internal.Dependency;

namespace GeoPlayClientSDK.Internal.Core.Dependency
{
    internal class DependencyChecker
    {
        private readonly IReadOnlyList<IDependencyCheck> _checks;

        /// <summary>
        /// Default constructor: initialize with built-in checks.
        /// For tests or advanced usage, pass explicit list of checks.
        /// </summary>
        public DependencyChecker(IEnumerable<IDependencyCheck> checks = null)
        {
            _checks = new IDependencyCheck[]
                    {
                    new ConfigFileDependency(),
                    new NetworkConnectionDependency()
                    };

            if (checks != null)
            {
                _checks = _checks.Concat(checks).ToArray();
            }
        }

        /// <summary>
        /// Run all checks and return overall DependencyStatus.
        /// Maintains backward-compatible signature but accepts a cancellation token.
        /// •A CancellationToken is a cooperative cancellation mechanism: it lets the caller request that an async operation stop early.
        /// The callee checks the token (or passes it to awaited APIs) and either stops work cleanly or throws OperationCanceledException.
        /// Usage Example: 
        /// using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        /// await dependencyChecker.CheckDependenciesAsync(cts.Token);
        /// </summary>
        public async Task<DependencyStatus> CheckDependenciesAsync(CancellationToken cancellationToken = default)
        {
            var results = await CheckAllAsync(cancellationToken).ConfigureAwait(false);

            // If any required dependency is unavailable, return its status (choose first failure)
            var failure = results.FirstOrDefault(r => !r.IsAvailable);
            if (failure != null)
            {
                Console.WriteLine($"[SDK] Dependency check failed: {failure.Name} -> {failure.Status} {failure.Message}");
                return failure.Status;
            }

            return DependencyStatus.Available;
        }

        /// <summary>
        /// Runs all checks and returns full per-dependency diagnostics.
        /// </summary>
        public async Task<IReadOnlyList<DependencyCheckResult>> CheckAllAsync(CancellationToken cancellationToken = default)
        {
            var tasks = _checks.Select(check => RunCheckSafeAsync(check, cancellationToken)).ToArray();
            var results = await Task.WhenAll(tasks).ConfigureAwait(false);
            return results;
        }

        private async Task<DependencyCheckResult> RunCheckSafeAsync(IDependencyCheck check, CancellationToken ct)
        {
            try
            {
                return await check.CheckAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return DependencyCheckResult.Unavailable(check.Name, DependencyStatus.UnavailableOther, "Check canceled");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SDK] Dependency '{check.Name}' threw: {ex.Message}");
                return DependencyCheckResult.Unavailable(check.Name, DependencyStatus.UnavailableOther, ex.Message, ex);
            }
        }
    }
}