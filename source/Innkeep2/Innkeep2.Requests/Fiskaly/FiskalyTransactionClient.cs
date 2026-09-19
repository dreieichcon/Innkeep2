using System.Text.Json;
using Innkeep2.Models.Core;
using Innkeep2.Models.Fiskaly.Transaction;
using Innkeep2.Requests.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Innkeep2.Requests.Fiskaly;

public sealed class FiskalyTransactionClient(
    HttpClient httpClient,
    [FromKeyedServices("fiskaly")] JsonSerializerOptions serializerOptions)
    : CoreApiClient(httpClient, serializerOptions)
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