using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GeoPlayClientSDK.Infrastructure.Network
{
    public interface IHttpClient
    {
        Task<TResponse> GetAsync<TResponse>(string endpoint, string service = "", Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default);
        Task<TResponse> PostAsync<TResponse>(string endpoint, string service, object body, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default);
        Task<TResponse> PutAsync<TResponse>(string endpoint, string service, object body, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default);
    }
}