using Demolite.Core.Models;
using Innkeep2.Models.Internal;
using Innkeep2.Models.Shared;
using Innkeep2.Requests.Cloud;
using Innkeep2.Services.Server;
using NSubstitute;

namespace Innkeep2.Services.Tests.Server;

[TestClass]
public class ServerPaymentServiceTests
{
    private readonly ICloudPaymentClient _cloud = Substitute.For<ICloudPaymentClient>();

    private ServerPaymentService CreateService() => new(_cloud);

    private static OrderRequest BuildOrder() => new()
    {
        RequestId = Guid.NewGuid(),
        Items = [new SalesItem { Id = 1, VariationId = 0, Name = "Item", Price = 10m, TaxRate = 19m, Quantity = 1 }],
        PaymentType = PaymentType.NonCash,
        AmountGiven = 10m,
        Currency = "EUR"
    };

    private static PaymentStatusResponse Pending(Guid id) => new(id, PaymentRequestStatus.Pending, "Bitte am Kartenleser bezahlen");

    private static Result<PaymentStatusResponse> CloudFailure(string code, string message)
        => Result<PaymentStatusResponse>.Failure(new Error(code, message));

    private const string ReaderBusyBody = """{"code":"Payment.ReaderBusy","message":"Der Kartenleser ist gerade in Benutzung"}""";

    [TestMethod]
    public async Task Start_ForwardsTheOrderAndReturnsTheCloudStatus()
    {
        var order = BuildOrder();
        var status = Pending(order.RequestId);
        _cloud.StartCardPaymentAsync(order, Arg.Any<CancellationToken>()).Returns(Result<PaymentStatusResponse>.Success(status));

        var result = await CreateService().StartAsync(order);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreSame(status, result.Value);
        await _cloud.Received(1).StartCardPaymentAsync(order, Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task GetStatus_ForwardsTheRequestIdAndReturnsTheCloudStatus()
    {
        var id = Guid.NewGuid();
        var status = new PaymentStatusResponse(id, PaymentRequestStatus.Completed);
        _cloud.GetCardPaymentAsync(id, Arg.Any<CancellationToken>()).Returns(Result<PaymentStatusResponse>.Success(status));

        var result = await CreateService().GetStatusAsync(id);

        Assert.AreSame(status, result.Value);
    }

    [TestMethod]
    public async Task Cancel_ForwardsTheRequestIdAndReturnsTheCloudStatus()
    {
        var id = Guid.NewGuid();
        var status = Pending(id);
        _cloud.CancelCardPaymentAsync(id, Arg.Any<CancellationToken>()).Returns(Result<PaymentStatusResponse>.Success(status));

        var result = await CreateService().CancelAsync(id);

        Assert.AreSame(status, result.Value);
    }

    [TestMethod]
    public async Task Calls_PassTheCancellationTokenOn()
    {
        using var cts = new CancellationTokenSource();
        var id = Guid.NewGuid();
        _cloud.GetCardPaymentAsync(id, cts.Token).Returns(Result<PaymentStatusResponse>.Success(Pending(id)));

        await CreateService().GetStatusAsync(id, cts.Token);

        await _cloud.Received(1).GetCardPaymentAsync(id, cts.Token);
    }

    [DataRow("Http.RequestFailed")]
    [DataRow("Http.Timeout")]
    [DataRow("Http.502")]
    [DataRow("Http.503")]
    [DataRow("Http.504")]
    [TestMethod]
    public async Task Errors_MeaningTheCloudIsUnreachableAreReportedAsSuch(string cloudCode)
    {
        _cloud.GetCardPaymentAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(CloudFailure(cloudCode, "raw body"));

        var result = await CreateService().GetStatusAsync(Guid.NewGuid());

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ServerPaymentService.CloudUnreachableCode, result.Error!.Code);
        StringAssert.Contains(result.Error.Message, "Cloud");
        Assert.DoesNotContain("raw body", result.Error.Message);
    }

    [DataRow("Http.401")]
    [DataRow("Http.403")]
    [TestMethod]
    public async Task Errors_ARejectedKeyIsReportedAsUnauthorized(string cloudCode)
    {
        _cloud.StartCardPaymentAsync(Arg.Any<OrderRequest>(), Arg.Any<CancellationToken>()).Returns(CloudFailure(cloudCode, ""));

        var result = await CreateService().StartAsync(BuildOrder());

        Assert.AreEqual(ServerPaymentService.CloudUnauthorizedCode, result.Error!.Code);
    }

    [TestMethod]
    public async Task Errors_KeepTheCodeAndMessageOfAPaymentErrorFromTheCloud()
    {
        _cloud.StartCardPaymentAsync(Arg.Any<OrderRequest>(), Arg.Any<CancellationToken>()).Returns(CloudFailure("Http.400", ReaderBusyBody));

        var result = await CreateService().StartAsync(BuildOrder());

        Assert.AreEqual("Payment.ReaderBusy", result.Error!.Code);
        Assert.AreEqual("Der Kartenleser ist gerade in Benutzung", result.Error.Message);
    }

    [TestMethod]
    public async Task Errors_KeepAPaymentNotFoundFromTheCloud()
    {
        _cloud.GetCardPaymentAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(CloudFailure("Http.404", """{"code":"Payment.NotFound","message":"Die Zahlung wurde nicht gefunden"}"""));

        var result = await CreateService().GetStatusAsync(Guid.NewGuid());

        Assert.AreEqual("Payment.NotFound", result.Error!.Code);
    }

    [TestMethod]
    public async Task Errors_ReadTheCloudErrorCaseInsensitive()
    {
        _cloud.StartCardPaymentAsync(Arg.Any<OrderRequest>(), Arg.Any<CancellationToken>())
            .Returns(CloudFailure("Http.400", """{"Code":"Payment.NoReader","Message":"Es ist kein aktiver SumUp-Reader hinterlegt","Extra":1}"""));

        var result = await CreateService().StartAsync(BuildOrder());

        Assert.AreEqual("Payment.NoReader", result.Error!.Code);
        Assert.AreEqual("Es ist kein aktiver SumUp-Reader hinterlegt", result.Error.Message);
    }

    [DataRow("not json")]
    [DataRow("")]
    [DataRow("[]")]
    [DataRow("\"text\"")]
    [DataRow("""{"message":"no code"}""")]
    [DataRow("""{"code":"Payment.X"}""")]
    [DataRow("""{"code":"Payment.X","message":"  "}""")]
    [DataRow("""{"code":5,"message":"x"}""")]
    [DataRow("""{"code":"Other.Error","message":"x"}""")]
    [TestMethod]
    public async Task Errors_ABadRequestWithoutAUsablePaymentErrorBecomesAGenericFailure(string body)
    {
        _cloud.StartCardPaymentAsync(Arg.Any<OrderRequest>(), Arg.Any<CancellationToken>()).Returns(CloudFailure("Http.400", body));

        var result = await CreateService().StartAsync(BuildOrder());

        Assert.AreEqual(ServerPaymentService.FailedCode, result.Error!.Code);
        Assert.AreEqual("Die Kartenzahlung konnte nicht bearbeitet werden", result.Error.Message);
    }

    [DataRow("Http.500")]
    [DataRow("Http.422")]
    [DataRow("Http.DeserializationFailed")]
    [DataRow("Http.EmptyBody")]
    [DataRow("Anything.Else")]
    [TestMethod]
    public async Task Errors_OtherFailuresBecomeAGenericFailureWithoutTheCloudDetails(string cloudCode)
    {
        _cloud.StartCardPaymentAsync(Arg.Any<OrderRequest>(), Arg.Any<CancellationToken>())
            .Returns(CloudFailure(cloudCode, ReaderBusyBody));

        var result = await CreateService().StartAsync(BuildOrder());

        Assert.AreEqual(ServerPaymentService.FailedCode, result.Error!.Code);
    }

    [DataRow("Payment.NotFound", 404)]
    [DataRow(ServerPaymentService.CloudUnreachableCode, 503)]
    [DataRow(ServerPaymentService.CloudUnauthorizedCode, 502)]
    [DataRow(ServerPaymentService.FailedCode, 400)]
    [DataRow("Payment.ReaderBusy", 400)]
    [DataRow("Payment.NotCard", 400)]
    [DataRow("Something.Else", 400)]
    [TestMethod]
    public void StatusCodeFor_MapsErrorsToHttpStatusCodes(string code, int expected)
        => Assert.AreEqual(expected, ServerPaymentService.StatusCodeFor(new Error(code, "message")));
}
