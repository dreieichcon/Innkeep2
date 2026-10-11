using System.Globalization;
using System.Text.Json;
using Demolite.Core.Models;
using Demolite.Http.Client;
using Innkeep2.Models.Sun;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Innkeep2.Requests.Sun;

public sealed class SunClient(HttpClient httpClient, [FromKeyedServices("sun")] JsonSerializerOptions serializerOptions, ILogger<SunClient> logger)
    : ApiClient(httpClient, serializerOptions, logger)
{
    public async Task<Result<SunState>> GetAsync(double lat, double lng, CancellationToken ct = default)
    {
        var latString = lat.ToString(CultureInfo.InvariantCulture);
        var lngString = lng.ToString(CultureInfo.InvariantCulture);

        var result = await GetAsync<SunStateResponse>(
            $"json?lat={latString}&lng={lngString}",
            ct
        );

        return result.IsSuccess
            ? Result<SunState>.Success(result.Value!.SunState)
            : Result<SunState>.Failure(result.Error!);
    }
}
