namespace GeoPlayClientSDK.Internal.Core.Dependency
{
    public enum DependencyStatus
    {
        Available,
        UnavailableMissing,
        UnavailableDisabled,
        UnavailableInvalid,
        UnavailablePermission,
        UnavailableUpdateRequired,
        UnavailableUpdating,
        UnavailableOther
    }
}
