using System.Text.Json;
using Demolite.Core.Models;
using Demolite.Http.Client;
using Innkeep2.Models.Fiskaly.Transaction;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Innkeep2.Requests.Fiskaly;

public sealed class FiskalyTransactionClient(
    HttpClient httpClient,
    [FromKeyedServices("fiskaly")] JsonSerializerOptions serializerOptions,
    ILogger<FiskalyTransactionClient> logger)
    : ApiClient(httpClient, serializerOptions, logger)
{
    public Task<Result<FiskalyTransaction>> StartAsync(
        Guid tssId,
        Guid txId,
        Guid clientId,
        CancellationToken ct = default
    )
        => PutAsync<FiskalyTransaction>(
            $"tss/{tssId}/tx/{txId}?tx_revision=1",
            new FiskalyTransactionUpdateRequest { State = TxState.Active, ClientId = clientId },
            ct
        );

    public Task<Result<FiskalyTransaction>> FinishAsync(
        Guid tssId,
        Guid txId,
        Guid clientId,
        int revision,
        FiskalyTransactionSchema schema,
        CancellationToken ct = default
    )
        => PutAsync<FiskalyTransaction>(
            $"tss/{tssId}/tx/{txId}?tx_revision={revision}",
            new FiskalyTransactionUpdateRequest { State = TxState.Finished, ClientId = clientId, Schema = schema },
            ct
        );
}
