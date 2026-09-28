using System.Globalization;
using System.Text.Json;
using Innkeep2.Models.Core;
using Innkeep2.Models.Sun;
using Innkeep2.Requests.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Innkeep2.Requests.Sun;

public sealed class SunClient(HttpClient httpClient, [FromKeyedServices("sun")] JsonSerializerOptions serializerOptions)
    : CoreApiClient(httpClient, serializerOptions)
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