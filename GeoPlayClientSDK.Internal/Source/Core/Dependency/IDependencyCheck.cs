using System.Threading;
using System.Threading.Tasks;

namespace GeoPlayClientSDK.Internal.Core.Dependency
{
    //
    internal interface IDependencyCheck
    {
        /// <summary>
        /// Short name of the dependency (e.g. "ConfigFile", "Network", "Permissions").
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Perform the check. Return a DependencyCheckResult describing the outcome.
        /// todo - Need to verify if async is needed here, or if synchronous is sufficient.
        /// </summary>
        Task<DependencyCheckResult> CheckAsync(CancellationToken cancellationToken = default);
    }
}