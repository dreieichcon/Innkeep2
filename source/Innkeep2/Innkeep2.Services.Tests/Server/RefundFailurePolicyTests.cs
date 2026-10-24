using Demolite.Core.Models;
using Innkeep2.Models.Shared;
using Innkeep2.Services.Server;

namespace Innkeep2.Services.Tests.Server;

[TestClass]
public class RefundFailurePolicyTests
{
    private const string CardBody = """{"code":"Refund.CardNotRefundable","message":"SumUp erlaubt die Rückerstattung dieser Zahlung nicht mehr"}""";

    [DataRow(PaymentType.Cash)]
    [DataRow(PaymentType.NonCash)]
    [TestMethod]
    public void Decide_AnOrderThatIsRefundedAlreadyIsReportedAsSuch(PaymentType paymentType)
    {
        var decision = RefundFailurePolicy.Decide(paymentType, new Error("Http.409", "body"));

        Assert.AreEqual(RefundFailureAction.AlreadyRefunded, decision.Action);
        Assert.AreEqual("Bestellung wurde bereits storniert.", decision.Message);
    }

    [DataRow("Http.RequestFailed")]
    [DataRow("Http.Timeout")]
    [DataRow("Http.500")]
    [DataRow("Http.502")]
    [DataRow("Http.503")]
    [DataRow("Http.400")]
    [DataRow("Http.422")]
    [TestMethod]
    public void Decide_CashStornosAreQueuedForEveryOtherFailure(string code)
    {
        var decision = RefundFailurePolicy.Decide(PaymentType.Cash, new Error(code, CardBody));

        Assert.AreEqual(RefundFailureAction.Queue, decision.Action);
        Assert.IsNull(decision.Message);
    }

    [DataRow("Http.RequestFailed")]
    [DataRow("Http.Timeout")]
    [DataRow("Http.502")]
    [DataRow("Http.503")]
    [DataRow("Http.504")]
    [TestMethod]
    public void Decide_ACardStornoIsNeverQueuedWhenTheCloudIsUnreachable(string code)
    {
        var decision = RefundFailurePolicy.Decide(PaymentType.NonCash, new Error(code, "<html>proxy error</html>"));

        Assert.AreEqual(RefundFailureAction.Reject, decision.Action);
        Assert.AreEqual(RefundFailurePolicy.CardOfflineMessage, decision.Message);
    }

    [TestMethod]
    public void Decide_ARefusedCardStornoShowsTheMessageOfTheCloud()
    {
        var decision = RefundFailurePolicy.Decide(PaymentType.NonCash, new Error("Http.422", CardBody));

        Assert.AreEqual(RefundFailureAction.Reject, decision.Action);
        Assert.AreEqual("SumUp erlaubt die Rückerstattung dieser Zahlung nicht mehr", decision.Message);
    }

    [TestMethod]
    public void Decide_ReadsTheCloudErrorCaseInsensitive()
    {
        var decision = RefundFailurePolicy.Decide(PaymentType.NonCash, new Error("Http.422", """{"Code":"Refund.CardRefundRejected","Message":"SumUp hat die Rückerstattung abgelehnt","Extra":1}"""));

        Assert.AreEqual("SumUp hat die Rückerstattung abgelehnt", decision.Message);
    }

    [TestMethod]
    public void Decide_OtherRefundErrorsOfTheCloudKeepTheirMessageForCardStornos()
    {
        var decision = RefundFailurePolicy.Decide(PaymentType.NonCash, new Error("Http.400", """{"code":"Refund.PretixOrderMissing","message":"Pretix Bestellung fehlt"}"""));

        Assert.AreEqual(RefundFailureAction.Reject, decision.Action);
        Assert.AreEqual("Pretix Bestellung fehlt", decision.Message);
    }

    [DataRow("not json")]
    [DataRow("")]
    [DataRow("[]")]
    [DataRow("\"text\"")]
    [DataRow("""{"message":"no code"}""")]
    [DataRow("""{"code":"Refund.CardNotRefundable"}""")]
    [DataRow("""{"code":"Refund.CardNotRefundable","message":" "}""")]
    [DataRow("""{"code":5,"message":"x"}""")]
    [DataRow("""{"code":"Other.Error","message":"internal detail"}""")]
    [TestMethod]
    public void Decide_ACardStornoWithoutAUsableCloudMessageGetsAGenericOne(string body)
    {
        var decision = RefundFailurePolicy.Decide(PaymentType.NonCash, new Error("Http.422", body));

        Assert.AreEqual(RefundFailureAction.Reject, decision.Action);
        Assert.AreEqual(RefundFailurePolicy.CardFailedMessage, decision.Message);
    }

    [TestMethod]
    public void Decide_ACardStornoNeverLeaksInternalDetailsOfOtherErrors()
    {
        var decision = RefundFailurePolicy.Decide(PaymentType.NonCash, new Error("Http.500", "stack trace with secrets"));

        Assert.DoesNotContain("secrets", decision.Message!);
    }
}
