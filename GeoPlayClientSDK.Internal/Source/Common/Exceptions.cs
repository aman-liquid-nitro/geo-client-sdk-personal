using System;

namespace GeoPlayClientSDK.Internal.Common
{
    public abstract class GeoPlayException : Exception
    {
        public GeoPlayException(string message) : base(message) { }
        public GeoPlayException(string message, Exception inner) : base(message, inner) { }
    }

    public class GeoPlaySdkException : GeoPlayException
    {
        public GeoPlaySdkException(string message) : base(message) { }
        public GeoPlaySdkException(string message, Exception inner) : base(message, inner) { }
    }

    public class NetworkException : GeoPlayException
    {
        public int StatusCode { get; }
        public NetworkException(string message, int statusCode) : base(message)
        {
            StatusCode = statusCode;
        }
    }
}