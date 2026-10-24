using Demolite.Core.Models;
using Innkeep2.Cloud.Services.Payments;
using Innkeep2.Cloud.TransactionDb.Models;
using Innkeep2.Cloud.TransactionDb.Repositories;
using Innkeep2.Models.Shared;
using Innkeep2.Models.SumUp;
using Innkeep2.Requests.SumUp;
using NSubstitute;

namespace Innkeep2.Cloud.Tests.Integration.Payments;

internal sealed class CardRefundServiceFixture : IDisposable
{
    public const string MerchantCode = "STORED01";
    public const string ClientTransactionId = "ctx-1";
    public const string SumUpTransactionId = "4ffb8dfc-7f2b-413d-a497-2ad00766585e";

    private readonly SqliteTransactionDbContextFactory _factory = new();

    public CardRefundServiceFixture()
    {
        Payments = new PaymentRequestRepository(_factory);
        Service = new CardRefundService(SumUp, Payments);

        TransactionIs();
        SumUp.RefundTransactionAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<decimal?>(), Arg.Any<CancellationToken>()).Returns(Result.Success());
    }

    public ISumUpClient SumUp { get; } = Substitute.For<ISumUpClient>();

    public PaymentRequestRepository Payments { get; }

    public CardRefundService Service { get; }

    /// <summary>
    /// Stores a completed card payment and returns its request id.
    /// </summary>
    public async Task<Guid> AddPaymentAsync(decimal total = 15m, string? clientTransactionId = ClientTransactionId, string merchantCode = MerchantCode)
    {
        var payment = new PaymentRequest
        {
            RequestId = Guid.NewGuid(),
            OrderRequestJson = "{}",
            TotalAmount = total,
            Currency = "EUR",
            MerchantCode = merchantCode,
            ReaderId = "rdr_1",
            Status = PaymentRequestStatus.Completed,
            ClientTransactionId = clientTransactionId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await Payments.CreateAsync(payment);

        return payment.RequestId;
    }

    public void TransactionIs(string status = "SUCCESSFUL", decimal? amount = 15m, decimal? refunded = null, string? id = SumUpTransactionId)
        => SumUp.GetTransactionAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<SumUpTransaction>.Success(new SumUpTransaction { Id = id, Status = status, Amount = amount, RefundedAmount = refunded, Currency = "EUR" }));

    public void LookupFails(string code)
        => SumUp.GetTransactionAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<SumUpTransaction>.Failure(new Error(code, "secret SumUp detail")));

    public void RefundFails(string code)
        => SumUp.RefundTransactionAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<decimal?>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error(code, "secret SumUp detail")));

    public void Dispose()
        => _factory.Dispose();
}
