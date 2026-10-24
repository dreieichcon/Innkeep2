using Innkeep2.Cloud.Services.Payments;
using Innkeep2.Cloud.TransactionDb.Models;
using Innkeep2.Models.Shared;

namespace Innkeep2.Cloud.Tests.Unit.Payments;

[TestClass]
public class CardPaymentRulesTests
{
    private static readonly DateTime Created = new(2026, 10, 24, 12, 0, 0, DateTimeKind.Utc);
    private static readonly CardPaymentOptions Options = new();

    private static PaymentRequest BuildRequest(PaymentRequestStatus status = PaymentRequestStatus.Pending, PaymentTerminationReason? reason = null) => new()
    {
        RequestId = Guid.NewGuid(),
        OrderRequestJson = "{}",
        TotalAmount = 10m,
        Currency = "EUR",
        MerchantCode = "MH4H92C7",
        ReaderId = "rdr_1",
        Status = status,
        TerminationReason = reason,
        CreatedAt = Created,
        UpdatedAt = Created
    };

    [DataRow("SUCCESSFUL", SumUpOutcome.Succeeded)]
    [DataRow("successful", SumUpOutcome.Succeeded)]
    [DataRow("FAILED", SumUpOutcome.Failed)]
    [DataRow("CANCELLED", SumUpOutcome.Failed)]
    [DataRow("PENDING", SumUpOutcome.Pending)]
    [DataRow("REFUNDED", SumUpOutcome.Unknown)]
    [DataRow("SOMETHING_NEW", SumUpOutcome.Unknown)]
    [DataRow("", SumUpOutcome.Unknown)]
    [DataRow(null, SumUpOutcome.Unknown)]
    [TestMethod]
    public void MapOutcome_MapsSumUpStatus(string? status, SumUpOutcome expected)
        => Assert.AreEqual(expected, CardPaymentRules.MapOutcome(status));

    [DataRow(null, PaymentRequestStatus.Failed)]
    [DataRow(PaymentTerminationReason.UserCancelled, PaymentRequestStatus.Cancelled)]
    [DataRow(PaymentTerminationReason.Timeout, PaymentRequestStatus.Expired)]
    [TestMethod]
    public void ResolveFailedStatus_UsesTheReasonWeTerminatedFor(PaymentTerminationReason? reason, PaymentRequestStatus expected)
        => Assert.AreEqual(expected, CardPaymentRules.ResolveFailedStatus(reason));

    [DataRow(PaymentRequestStatus.Pending, false)]
    [DataRow(PaymentRequestStatus.Paid, false)]
    [DataRow(PaymentRequestStatus.Completed, true)]
    [DataRow(PaymentRequestStatus.Failed, true)]
    [DataRow(PaymentRequestStatus.Cancelled, true)]
    [DataRow(PaymentRequestStatus.Expired, true)]
    [TestMethod]
    public void IsFinal_IsTrueOnlyForEndStates(PaymentRequestStatus status, bool expected)
        => Assert.AreEqual(expected, CardPaymentRules.IsFinal(status));

    [DataRow(PaymentRequestStatus.Pending, PaymentRequestStatus.Paid, true)]
    [DataRow(PaymentRequestStatus.Pending, PaymentRequestStatus.Failed, true)]
    [DataRow(PaymentRequestStatus.Pending, PaymentRequestStatus.Cancelled, true)]
    [DataRow(PaymentRequestStatus.Pending, PaymentRequestStatus.Expired, true)]
    [DataRow(PaymentRequestStatus.Paid, PaymentRequestStatus.Completed, true)]
    [DataRow(PaymentRequestStatus.Pending, PaymentRequestStatus.Completed, false)]
    [DataRow(PaymentRequestStatus.Pending, PaymentRequestStatus.Pending, false)]
    [DataRow(PaymentRequestStatus.Paid, PaymentRequestStatus.Failed, false)]
    [DataRow(PaymentRequestStatus.Paid, PaymentRequestStatus.Cancelled, false)]
    [DataRow(PaymentRequestStatus.Paid, PaymentRequestStatus.Expired, false)]
    [DataRow(PaymentRequestStatus.Paid, PaymentRequestStatus.Pending, false)]
    [DataRow(PaymentRequestStatus.Completed, PaymentRequestStatus.Failed, false)]
    [DataRow(PaymentRequestStatus.Completed, PaymentRequestStatus.Paid, false)]
    [DataRow(PaymentRequestStatus.Failed, PaymentRequestStatus.Paid, false)]
    [DataRow(PaymentRequestStatus.Cancelled, PaymentRequestStatus.Paid, false)]
    [DataRow(PaymentRequestStatus.Expired, PaymentRequestStatus.Paid, false)]
    [DataRow(PaymentRequestStatus.Failed, PaymentRequestStatus.Completed, false)]
    [TestMethod]
    public void CanTransition_AllowsOnlyTheDefinedTransitions(PaymentRequestStatus from, PaymentRequestStatus to, bool expected)
        => Assert.AreEqual(expected, CardPaymentRules.CanTransition(from, to));

    [TestMethod]
    public void CanTransition_NeverLeavesAFinalStatus()
    {
        foreach (var from in Enum.GetValues<PaymentRequestStatus>().Where(CardPaymentRules.IsFinal))
        foreach (var to in Enum.GetValues<PaymentRequestStatus>())
            Assert.IsFalse(CardPaymentRules.CanTransition(from, to), $"{from} -> {to}");
    }

    [TestMethod]
    public void ShouldVerify_IsFalseBeforeTheFirstDelay()
    {
        var now = Created + Options.FirstVerificationDelay - TimeSpan.FromMilliseconds(1);

        Assert.IsFalse(CardPaymentRules.ShouldVerify(BuildRequest(), now, null, Options));
    }

    [TestMethod]
    public void ShouldVerify_IsTrueAtTheFirstDelayWithoutEarlierVerification()
    {
        var now = Created + Options.FirstVerificationDelay;

        Assert.IsTrue(CardPaymentRules.ShouldVerify(BuildRequest(), now, null, Options));
    }

    [TestMethod]
    public void ShouldVerify_IsFalseWithinTheIntervalAfterTheLastVerification()
    {
        var now = Created + TimeSpan.FromSeconds(60);
        var last = now - Options.VerificationInterval + TimeSpan.FromMilliseconds(1);

        Assert.IsFalse(CardPaymentRules.ShouldVerify(BuildRequest(), now, last, Options));
    }

    [TestMethod]
    public void ShouldVerify_IsTrueOnceTheIntervalHasPassed()
    {
        var now = Created + TimeSpan.FromSeconds(60);
        var last = now - Options.VerificationInterval;

        Assert.IsTrue(CardPaymentRules.ShouldVerify(BuildRequest(), now, last, Options));
    }

    [DataRow(PaymentRequestStatus.Paid)]
    [DataRow(PaymentRequestStatus.Completed)]
    [DataRow(PaymentRequestStatus.Failed)]
    [DataRow(PaymentRequestStatus.Cancelled)]
    [DataRow(PaymentRequestStatus.Expired)]
    [TestMethod]
    public void ShouldVerify_IsFalseForRequestsThatAreNotPending(PaymentRequestStatus status)
    {
        var now = Created + TimeSpan.FromMinutes(5);

        Assert.IsFalse(CardPaymentRules.ShouldVerify(BuildRequest(status), now, null, Options));
    }

    [TestMethod]
    public void ShouldTerminateForTimeout_IsFalseBeforeTheOverallTimeout()
    {
        var now = Created + Options.OverallTimeout - TimeSpan.FromMilliseconds(1);

        Assert.IsFalse(CardPaymentRules.ShouldTerminateForTimeout(BuildRequest(), now, Options));
    }

    [TestMethod]
    public void ShouldTerminateForTimeout_IsTrueAtTheOverallTimeout()
    {
        var now = Created + Options.OverallTimeout;

        Assert.IsTrue(CardPaymentRules.ShouldTerminateForTimeout(BuildRequest(), now, Options));
    }

    [DataRow(PaymentTerminationReason.UserCancelled)]
    [DataRow(PaymentTerminationReason.Timeout)]
    [TestMethod]
    public void ShouldTerminateForTimeout_IsFalseWhenTerminationWasAlreadyRequested(PaymentTerminationReason reason)
    {
        var now = Created + Options.OverallTimeout + TimeSpan.FromMinutes(1);

        Assert.IsFalse(CardPaymentRules.ShouldTerminateForTimeout(BuildRequest(reason: reason), now, Options));
    }

    [DataRow(PaymentRequestStatus.Paid)]
    [DataRow(PaymentRequestStatus.Completed)]
    [DataRow(PaymentRequestStatus.Failed)]
    [TestMethod]
    public void ShouldTerminateForTimeout_IsFalseForRequestsThatAreNotPending(PaymentRequestStatus status)
    {
        var now = Created + Options.OverallTimeout + TimeSpan.FromMinutes(1);

        Assert.IsFalse(CardPaymentRules.ShouldTerminateForTimeout(BuildRequest(status), now, Options));
    }

    [TestMethod]
    public void ReaderBusyCutoff_IsTheBusyWindowBeforeNow()
    {
        var now = Created.AddMinutes(10);

        Assert.AreEqual(now - Options.ReaderBusyWindow, CardPaymentRules.ReaderBusyCutoff(now, Options));
    }

    [TestMethod]
    public void Options_HaveTheAgreedDefaults()
    {
        Assert.AreEqual(TimeSpan.FromSeconds(150), Options.OverallTimeout);
        Assert.AreEqual(TimeSpan.FromSeconds(10), Options.FirstVerificationDelay);
        Assert.AreEqual(TimeSpan.FromSeconds(5), Options.VerificationInterval);
        Assert.IsGreaterThan(Options.OverallTimeout, Options.ReaderBusyWindow);
    }

    [TestMethod]
    public void Options_CanBeTweaked()
    {
        var options = new CardPaymentOptions { OverallTimeout = TimeSpan.FromSeconds(30) };
        var now = Created + TimeSpan.FromSeconds(30);

        Assert.IsTrue(CardPaymentRules.ShouldTerminateForTimeout(BuildRequest(), now, options));
    }

    [TestMethod]
    public void GetAttention_APendingPaymentIsNormalWhileTheReaderIsStillOccupied()
    {
        var now = Created + Options.ReaderBusyWindow - TimeSpan.FromMilliseconds(1);

        Assert.AreEqual(PaymentAttention.None, CardPaymentRules.GetAttention(BuildRequest(), false, now, Options));
    }

    [TestMethod]
    public void GetAttention_APendingPaymentOlderThanTheBusyWindowIsHanging()
    {
        var now = Created + Options.ReaderBusyWindow;

        Assert.AreEqual(PaymentAttention.HangingPending, CardPaymentRules.GetAttention(BuildRequest(), false, now, Options));
    }

    [DataRow(true)]
    [DataRow(false)]
    [TestMethod]
    public void GetAttention_APaidPaymentNeedsAttentionWithOrWithoutAnOrder(bool hasOrder)
        => Assert.AreEqual(PaymentAttention.PaidNotCompleted, CardPaymentRules.GetAttention(BuildRequest(PaymentRequestStatus.Paid), hasOrder, Created, Options));

    [TestMethod]
    public void GetAttention_ACompletedPaymentWithoutAnOrderNeedsAttention()
        => Assert.AreEqual(PaymentAttention.CompletedWithoutOrder, CardPaymentRules.GetAttention(BuildRequest(PaymentRequestStatus.Completed), false, Created, Options));

    [TestMethod]
    public void GetAttention_ACompletedPaymentWithAnOrderIsFine()
        => Assert.AreEqual(PaymentAttention.None, CardPaymentRules.GetAttention(BuildRequest(PaymentRequestStatus.Completed), true, Created, Options));

    [DataRow(PaymentRequestStatus.Failed)]
    [DataRow(PaymentRequestStatus.Cancelled)]
    [DataRow(PaymentRequestStatus.Expired)]
    [TestMethod]
    public void GetAttention_EndedPaymentsAreFineEvenWithoutAnOrder(PaymentRequestStatus status)
    {
        var now = Created + TimeSpan.FromDays(3);

        Assert.AreEqual(PaymentAttention.None, CardPaymentRules.GetAttention(BuildRequest(status), false, now, Options));
    }
}
