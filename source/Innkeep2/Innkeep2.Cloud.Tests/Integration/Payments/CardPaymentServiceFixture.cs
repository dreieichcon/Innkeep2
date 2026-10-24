using Demolite.Core.Models;
using Innkeep2.Cloud.Services.Payments;
using Innkeep2.Cloud.Services.Transactions;
using Innkeep2.Cloud.TransactionDb.Models;
using Innkeep2.Cloud.TransactionDb.Repositories;
using Innkeep2.CredentialsDb;
using Innkeep2.CredentialsDb.Interfaces;
using Innkeep2.CredentialsDb.Models;
using Innkeep2.Models.Internal;
using Innkeep2.Models.Internal.Receipt;
using Innkeep2.Models.Shared;
using Innkeep2.Models.SumUp;
using Innkeep2.Requests.SumUp;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Innkeep2.Cloud.Tests.Integration.Payments;

internal sealed class CardPaymentServiceFixture : IDisposable
{
    public const string MerchantCode = "MH4H92C7";
    public const string ReaderId = "rdr_1";
    public const string WebhookToken = "secret-token";
    public const string ClientTransactionId = "ctx-1";

    private readonly SqliteTransactionDbContextFactory _factory = new();

    private CardPaymentServiceFixture(SumUpCredential? account, SumUpReaderCredential? reader, CardPaymentOptions options)
    {
        Options = options;
        Payments = new PaymentRequestRepository(_factory);

        var accountRepository = Substitute.For<ICredentialRepository<SumUpCredential>>();
        accountRepository.GetActiveAsync().Returns(account is null
            ? Result<SumUpCredential>.Failure(new Error("NotFound", "none"))
            : Result<SumUpCredential>.Success(account));
        Account = new ActiveCredentialProvider<SumUpCredential>(accountRepository);

        var readerRepository = Substitute.For<ICredentialRepository<SumUpReaderCredential>>();
        readerRepository.GetActiveAsync().Returns(reader is null
            ? Result<SumUpReaderCredential>.Failure(new Error("NotFound", "none"))
            : Result<SumUpReaderCredential>.Success(reader));
        Reader = new ActiveCredentialProvider<SumUpReaderCredential>(readerRepository);

        SumUp.CreateCheckoutAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<SumUpCheckoutRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<SumUpCheckoutResponse>.Success(new SumUpCheckoutResponse { Data = new SumUpCheckoutData { ClientTransactionId = ClientTransactionId, CheckoutId = "chk-1" } }));
        SumUp.TerminateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Result.Success());
        TransactionIs("PENDING");
        Orders.CreateOrderAsync(Arg.Any<OrderRequest>(), Arg.Any<CancellationToken>(), Arg.Any<bool>(), Arg.Any<string?>())
            .Returns(Result<TransactionReceipt>.Success(BuildReceipt()));
        Orders.GetReceiptAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(BuildReceipt());

        Service = new CardPaymentService(SumUp, Payments, Orders, Account, Reader, options, Time);
    }

    public FakeTimeProvider Time { get; } = new(new DateTimeOffset(2026, 10, 24, 12, 0, 0, TimeSpan.Zero));

    public CardPaymentOptions Options { get; }

    public PaymentRequestRepository Payments { get; }

    public ISumUpClient SumUp { get; } = Substitute.For<ISumUpClient>();

    public IOrderCreationService Orders { get; } = Substitute.For<IOrderCreationService>();

    public ActiveCredentialProvider<SumUpCredential> Account { get; }

    public ActiveCredentialProvider<SumUpReaderCredential> Reader { get; }

    public CardPaymentService Service { get; }

    /// <summary>
    /// Creates the fixture with an active account and reader, unless told otherwise.
    /// </summary>
    public static async Task<CardPaymentServiceFixture> CreateAsync(
        bool withAccount = true,
        bool withReader = true,
        SumUpCredential? account = null,
        SumUpReaderCredential? reader = null,
        CardPaymentOptions? options = null)
    {
        var fixture = new CardPaymentServiceFixture(
            withAccount ? account ?? BuildAccount() : null,
            withReader ? reader ?? BuildReader() : null,
            options ?? new CardPaymentOptions());

        await fixture.Account.ReloadAsync();
        await fixture.Reader.ReloadAsync();

        return fixture;
    }

    public static SumUpCredential BuildAccount() => new()
    {
        Name = "Account",
        Active = true,
        ApiKey = "key",
        MerchantCode = MerchantCode,
        PublicBaseUrl = "https://cloud.example",
        WebhookToken = WebhookToken
    };

    public static SumUpReaderCredential BuildReader(string merchantCode = MerchantCode) => new()
    {
        Name = "Front",
        Active = true,
        ReaderId = ReaderId,
        MerchantCode = merchantCode
    };

    public static OrderRequest BuildOrder(Guid? requestId = null, decimal price = 15m, PaymentType paymentType = PaymentType.NonCash, decimal? amountGiven = null) => new()
    {
        RequestId = requestId ?? Guid.NewGuid(),
        Items = [new SalesItem { Id = 1, VariationId = 0, Name = "Item", Price = price, TaxRate = 19m, Quantity = 1 }],
        PaymentType = paymentType,
        AmountGiven = amountGiven ?? price,
        Currency = "EUR"
    };

    public static TransactionReceipt BuildReceipt() => new()
    {
        OrderId = Guid.NewGuid(),
        TransactionType = TransactionType.Sale,
        Title = "Title",
        Header = "Header",
        BookingTime = DateTime.UtcNow,
        Currency = "EUR",
        Lines = [],
        Sum = new ReceiptSum { TotalAmount = 15m, AmountGiven = 15m, AmountReturned = 0m },
        TaxInformation = [],
        Vouchers = [],
        PaymentType = PaymentType.NonCash
    };

    /// <summary>
    /// Starts a payment and returns its request id.
    /// </summary>
    public async Task<Guid> StartAsync(OrderRequest? order = null)
    {
        order ??= BuildOrder();
        await Service.StartAsync(order);

        return order.RequestId;
    }

    public void TransactionIs(string status, decimal? amount = 15m, string? currency = "EUR", string? code = "TEENSK4W2K")
        => SumUp.GetTransactionAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<SumUpTransaction>.Success(new SumUpTransaction { Status = status, Amount = amount, Currency = currency, TransactionCode = code, ClientTransactionId = ClientTransactionId }));

    public void LookupFails()
        => SumUp.GetTransactionAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<SumUpTransaction>.Failure(new Error("Http.404", "not found")));

    public async Task<PaymentRequest> LoadAsync(Guid requestId)
        => (await Payments.GetByRequestIdAsync(requestId)).Value!;

    public void Advance(TimeSpan time)
        => Time.Advance(time);

    public void Dispose()
        => _factory.Dispose();
}
