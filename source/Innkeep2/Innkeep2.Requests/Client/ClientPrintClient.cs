using System.Text.Json;
using Innkeep2.Models.Core;
using Innkeep2.Models.Internal.Receipt;
using Innkeep2.Requests.Core;

namespace Innkeep2.Requests.Client;

public sealed class ClientPrintClient(HttpClient httpClient)
    : CoreApiClient(httpClient, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
{
    public Task<Result<Unit>> PrintReceiptAsync(TransactionReceipt receipt, CancellationToken ct = default)
        => PostAsync<Unit>("print", receipt, ct);
}