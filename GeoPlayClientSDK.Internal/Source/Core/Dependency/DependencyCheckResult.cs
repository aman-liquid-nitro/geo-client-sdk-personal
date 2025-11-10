using System;

namespace GeoPlayClientSDK.Internal.Core.Dependency
{
    internal class DependencyCheckResult
    {
        public string Name { get; set; }
        public bool IsAvailable { get; set; }
        public DependencyStatus Status { get; set; }
        public string Message { get; set; }
        public Exception Exception { get; set; }

        public static DependencyCheckResult Available(string name) =>
            new DependencyCheckResult { Name = name, IsAvailable = true, Status = DependencyStatus.Available };

        public static DependencyCheckResult Unavailable(string name, DependencyStatus status, string message = null, Exception ex = null) =>
            new DependencyCheckResult { Name = name, IsAvailable = false, Status = status, Message = message, Exception = ex };
    }
}