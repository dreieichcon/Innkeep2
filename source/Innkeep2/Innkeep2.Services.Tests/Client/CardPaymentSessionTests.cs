using Innkeep2.Models.Internal;
using Innkeep2.Models.Shared;
using Innkeep2.Services.Client;
using NSubstitute;
using static Innkeep2.Services.Tests.Client.CardPaymentSessionFixture;

namespace Innkeep2.Services.Tests.Client;

[TestClass]
public class CardPaymentSessionTests
{
    [TestMethod]
    public void NewSession_WaitsForTheCashierAndAllowsStartAndCash()
    {
        var fixture = new CardPaymentSessionFixture();

        Assert.AreEqual(CardPaymentPhase.NotStarted, fixture.Session.Phase);
        Assert.AreEqual("Bereit zur Zahlung", fixture.Session.StatusText);
        Assert.IsTrue(fixture.Session.CanStart);
        Assert.IsTrue(fixture.Session.CanSwitchToCash);
        Assert.IsFalse(fixture.Session.CanCancel);
        Assert.IsFalse(fixture.Session.IsFinished);
        Assert.AreEqual(15m, fixture.Session.Amount);
    }

    [TestMethod]
    public async Task Start_DoesNotHappenUntilTheCashierStartsIt()
    {
        var fixture = new CardPaymentSessionFixture();

        await Task.Yield();

        await fixture.Client.DidNotReceiveWithAnyArgs().StartCardPaymentAsync(default!, default);
    }

    [TestMethod]
    public async Task Start_SendsTheCartAsANonCashOrderWithTheTotalAsGivenAmount()
    {
        var fixture = new CardPaymentSessionFixture(price: 12.5m);
        fixture.StatusReturns(Success(Guid.Empty, PaymentRequestStatus.Failed));

        await fixture.Session.StartAsync();

        await fixture.Client.Received(1).StartCardPaymentAsync(
            Arg.Is<OrderRequest>(x => x.PaymentType == PaymentType.NonCash
                                      && x.AmountGiven == 12.5m
                                      && x.AmountNeeded == 12.5m
                                      && x.Currency == "EUR"
                                      && x.Items.Count == 1
                                      && x.RequestId != Guid.Empty),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Start_ShowsTheStartingStatusWhileTheStartIsRunning()
    {
        var fixture = new CardPaymentSessionFixture();
        var phases = new List<CardPaymentPhase>();
        fixture.Session.Changed += (_, _) => phases.Add(fixture.Session.Phase);
        fixture.StatusReturns(Success(Guid.Empty, PaymentRequestStatus.Failed));

        await fixture.Session.StartAsync();

        Assert.AreEqual(CardPaymentPhase.Starting, phases.First());
    }

    [TestMethod]
    public async Task Start_FollowsThePaymentUntilTheReceiptArrives()
    {
        var fixture = new CardPaymentSessionFixture();
        var receipt = BuildReceipt();
        fixture.StatusReturns(
            Success(Guid.Empty, PaymentRequestStatus.Pending, "Bitte am Kartenleser bezahlen"),
            Success(Guid.Empty, PaymentRequestStatus.Paid, "Zahlung erhalten, die Bestellung wird angelegt"),
            Success(Guid.Empty, PaymentRequestStatus.Completed, null, receipt));

        await fixture.Session.StartAsync();

        Assert.AreEqual(CardPaymentPhase.Completed, fixture.Session.Phase);
        Assert.AreSame(receipt, fixture.Session.Receipt);
        Assert.AreEqual("Zahlung erfolgreich", fixture.Session.StatusText);
        Assert.IsTrue(fixture.Session.IsFinished);
        Assert.IsFalse(fixture.Session.CanStart);
        Assert.IsFalse(fixture.Session.CanSwitchToCash);
        Assert.IsFalse(fixture.Session.CanCancel);
        fixture.Registry.Received(1).RegisterCompletedOrder(receipt);
        await fixture.Client.Received(3).GetCardPaymentAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Start_ReturnsTheStatusOfTheStartedRequest()
    {
        var fixture = new CardPaymentSessionFixture();
        fixture.StatusReturns(Success(Guid.Empty, PaymentRequestStatus.Failed));

        await fixture.Session.StartAsync();

        var started = (OrderRequest)fixture.Client.ReceivedCalls().First(x => x.GetMethodInfo().Name == "StartCardPaymentAsync").GetArguments()[0]!;
        await fixture.Client.Received(1).GetCardPaymentAsync(started.RequestId, Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Start_FinishesWithoutPollingWhenTheStartAnswerIsAlreadyFinal()
    {
        var fixture = new CardPaymentSessionFixture();
        fixture.StartReturns(Success(Guid.NewGuid(), PaymentRequestStatus.Completed, null, BuildReceipt()));

        await fixture.Session.StartAsync();

        Assert.AreEqual(CardPaymentPhase.Completed, fixture.Session.Phase);
        await fixture.Client.DidNotReceiveWithAnyArgs().GetCardPaymentAsync(default, default);
    }

    [TestMethod]
    public async Task Poll_WaitsThePollIntervalBeforeEachRequest()
    {
        var fixture = new CardPaymentSessionFixture();
        fixture.StatusReturns(
            Success(Guid.Empty, PaymentRequestStatus.Pending),
            Success(Guid.Empty, PaymentRequestStatus.Failed));

        await fixture.Session.StartAsync();

        Assert.HasCount(2, fixture.Waits);
        Assert.IsTrue(fixture.Waits.All(x => x == fixture.Options.PollInterval));
        Assert.AreEqual(TimeSpan.FromSeconds(1.5), fixture.Options.PollInterval);
    }

    [DataRow(PaymentRequestStatus.Failed, "Die Zahlung ist fehlgeschlagen")]
    [DataRow(PaymentRequestStatus.Cancelled, "Die Zahlung wurde abgebrochen")]
    [DataRow(PaymentRequestStatus.Expired, "Zeitüberschreitung, die Zahlung wurde nicht abgeschlossen")]
    [TestMethod]
    public async Task ConfirmedFailure_ShowsTheMessageAndAllowsAnotherAttemptOrCash(PaymentRequestStatus status, string message)
    {
        var fixture = new CardPaymentSessionFixture();
        fixture.StatusReturns(Success(Guid.Empty, status, message));

        await fixture.Session.StartAsync();

        Assert.AreEqual(CardPaymentPhase.Failed, fixture.Session.Phase);
        Assert.AreEqual(message, fixture.Session.StatusText);
        Assert.IsTrue(fixture.Session.CanStart);
        Assert.IsTrue(fixture.Session.CanSwitchToCash);
        Assert.IsFalse(fixture.Session.CanCancel);
        Assert.IsFalse(fixture.Session.IsFinished);
        fixture.Registry.DidNotReceiveWithAnyArgs().RegisterCompletedOrder(default!);
    }

    [TestMethod]
    public async Task ConfirmedFailure_UsesADefaultTextWithoutAMessage()
    {
        var fixture = new CardPaymentSessionFixture();
        fixture.StatusReturns(Success(Guid.Empty, PaymentRequestStatus.Failed));

        await fixture.Session.StartAsync();

        Assert.AreEqual("Die Zahlung ist fehlgeschlagen", fixture.Session.StatusText);
    }

    [TestMethod]
    public async Task AnotherAttempt_UsesANewRequestId()
    {
        var fixture = new CardPaymentSessionFixture();
        fixture.StatusReturns(Success(Guid.Empty, PaymentRequestStatus.Failed));
        await fixture.Session.StartAsync();

        await fixture.Session.StartAsync();

        var ids = fixture.Client.ReceivedCalls()
            .Where(x => x.GetMethodInfo().Name == "StartCardPaymentAsync")
            .Select(x => ((OrderRequest)x.GetArguments()[0]!).RequestId)
            .ToList();
        Assert.HasCount(2, ids);
        Assert.AreNotEqual(ids[0], ids[1]);
    }

    [TestMethod]
    public async Task Start_IsIgnoredWhileAPaymentIsRunning()
    {
        var fixture = new CardPaymentSessionFixture();
        Task? second = null;
        fixture.Client.GetCardPaymentAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            second = fixture.Session.StartAsync();
            return Success(Guid.Empty, PaymentRequestStatus.Failed);
        });

        await fixture.Session.StartAsync();
        await second!;

        await fixture.Client.Received(1).StartCardPaymentAsync(Arg.Any<OrderRequest>(), Arg.Any<CancellationToken>());
    }

    [DataRow("Payment.ReaderBusy", "Der Kartenleser ist gerade in Benutzung")]
    [DataRow("Payment.NoReader", "Es ist kein aktiver SumUp-Reader hinterlegt")]
    [DataRow("Payment.NoAccount", "Es sind keine aktiven SumUp-Zugangsdaten hinterlegt")]
    [DataRow("Payment.AmountMismatch", "Summe und gegebener Betrag müssen übereinstimmen")]
    [TestMethod]
    public async Task RefusedStart_ShowsTheReasonAndAllowsAnotherAttemptOrCashWithoutPolling(string code, string message)
    {
        var fixture = new CardPaymentSessionFixture();
        fixture.StartReturns(Failure(code, message));

        await fixture.Session.StartAsync();

        Assert.AreEqual(CardPaymentPhase.Failed, fixture.Session.Phase);
        Assert.AreEqual(message, fixture.Session.StatusText);
        Assert.IsTrue(fixture.Session.CanStart);
        Assert.IsTrue(fixture.Session.CanSwitchToCash);
        await fixture.Client.DidNotReceiveWithAnyArgs().GetCardPaymentAsync(default, default);
    }

    [DataRow("Payment.CloudUnreachable")]
    [DataRow("Payment.Failed")]
    [DataRow("Http.RequestFailed")]
    [DataRow("Http.Timeout")]
    [DataRow("Http.500")]
    [TestMethod]
    public async Task UncertainStart_ChecksWhetherThePaymentExistsBeforeAllowingAnything(string code)
    {
        var fixture = new CardPaymentSessionFixture();
        fixture.StartReturns(Failure(code));
        fixture.StatusReturns(Failure("Payment.CloudUnreachable"));

        var run = fixture.Session.StartAsync();
        await run;

        await fixture.Client.Received().GetCardPaymentAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        Assert.AreNotEqual(CardPaymentPhase.Failed, fixture.Session.Phase);
        Assert.IsFalse(fixture.Session.CanSwitchToCash);
        Assert.IsFalse(fixture.Session.CanStart);
    }

    [TestMethod]
    public async Task UncertainStart_AdoptsAPaymentThatTheCloudKnows()
    {
        var fixture = new CardPaymentSessionFixture();
        var receipt = BuildReceipt();
        fixture.StartReturns(Failure("Http.Timeout"));
        fixture.StatusReturns(
            Success(Guid.Empty, PaymentRequestStatus.Pending, "Bitte am Kartenleser bezahlen"),
            Success(Guid.Empty, PaymentRequestStatus.Completed, null, receipt));

        await fixture.Session.StartAsync();

        Assert.AreEqual(CardPaymentPhase.Completed, fixture.Session.Phase);
        Assert.AreSame(receipt, fixture.Session.Receipt);
    }

    [TestMethod]
    public async Task UncertainStart_ConcludesThePaymentDoesNotExistAfterTwoNotFoundAnswers()
    {
        var fixture = new CardPaymentSessionFixture();
        fixture.StartReturns(Failure("Http.Timeout"));
        fixture.StatusReturns(Failure("Payment.NotFound"));

        await fixture.Session.StartAsync();

        Assert.AreEqual(CardPaymentPhase.Failed, fixture.Session.Phase);
        Assert.AreEqual("Die Zahlung wurde nicht gestartet, bitte erneut versuchen", fixture.Session.StatusText);
        Assert.IsTrue(fixture.Session.CanStart);
        Assert.IsTrue(fixture.Session.CanSwitchToCash);
        await fixture.Client.Received(2).GetCardPaymentAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task UncertainStart_AOneTimeNotFoundIsNotEnough()
    {
        var fixture = new CardPaymentSessionFixture();
        fixture.StartReturns(Failure("Http.Timeout"));
        fixture.StatusReturns(
            Failure("Payment.NotFound"),
            Success(Guid.Empty, PaymentRequestStatus.Failed, "Die Zahlung ist fehlgeschlagen"));

        await fixture.Session.StartAsync();

        Assert.AreEqual("Die Zahlung ist fehlgeschlagen", fixture.Session.StatusText);
    }

    [TestMethod]
    public async Task UncertainStart_UnreachableAnswersBetweenNotFoundAnswersDoNotHideThem()
    {
        var fixture = new CardPaymentSessionFixture();
        fixture.StartReturns(Failure("Http.Timeout"));
        fixture.StatusReturns(
            Failure("Payment.NotFound"),
            Failure("Payment.CloudUnreachable"),
            Failure("Payment.NotFound"),
            Success(Guid.Empty, PaymentRequestStatus.Pending));

        await fixture.Session.StartAsync();

        Assert.AreEqual(CardPaymentPhase.Failed, fixture.Session.Phase);
        await fixture.Client.Received(3).GetCardPaymentAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task KnownPayment_NeverConcludesItDoesNotExistFromNotFound()
    {
        var fixture = new CardPaymentSessionFixture();
        fixture.StatusReturns(Failure("Payment.NotFound"));

        await fixture.Session.StartAsync();

        Assert.AreEqual(CardPaymentPhase.GaveUp, fixture.Session.Phase);
        Assert.IsFalse(fixture.Session.CanSwitchToCash);
    }

    [TestMethod]
    public async Task UnreachableCloud_KeepsPollingWithoutAllowingCash()
    {
        var fixture = new CardPaymentSessionFixture();
        fixture.StatusReturns(
            Failure("Payment.CloudUnreachable"),
            Failure("Payment.CloudUnreachable"),
            Success(Guid.Empty, PaymentRequestStatus.Failed, "Die Zahlung ist fehlgeschlagen"));
        var texts = new List<string>();
        fixture.Session.Changed += (_, _) => texts.Add(fixture.Session.StatusText);

        await fixture.Session.StartAsync();

        CollectionAssert.Contains(texts, "Verbindung zur Cloud gestört, warte auf die Bestätigung ...");
        Assert.AreEqual(CardPaymentPhase.Failed, fixture.Session.Phase);
        await fixture.Client.Received(3).GetCardPaymentAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task OtherPollErrors_KeepPollingWithAGenericText()
    {
        var fixture = new CardPaymentSessionFixture();
        fixture.StatusReturns(
            Failure("Http.500"),
            Success(Guid.Empty, PaymentRequestStatus.Failed));
        var texts = new List<string>();
        fixture.Session.Changed += (_, _) => texts.Add(fixture.Session.StatusText);

        await fixture.Session.StartAsync();

        CollectionAssert.Contains(texts, "Der Status der Zahlung kann nicht abgefragt werden, warte auf die Bestätigung ...");
    }

    [TestMethod]
    public async Task CompletedWithoutAReceipt_KeepsPollingUntilTheReceiptArrives()
    {
        var fixture = new CardPaymentSessionFixture();
        var receipt = BuildReceipt();
        fixture.StatusReturns(
            Success(Guid.Empty, PaymentRequestStatus.Completed, null, null),
            Success(Guid.Empty, PaymentRequestStatus.Completed, null, receipt));

        await fixture.Session.StartAsync();

        Assert.AreSame(receipt, fixture.Session.Receipt);
        fixture.Registry.Received(1).RegisterCompletedOrder(receipt);
    }

    [TestMethod]
    public async Task GiveUp_StopsAfterTheConfiguredTimeWithoutAFinalStatus()
    {
        var fixture = new CardPaymentSessionFixture();

        await fixture.Session.StartAsync();

        Assert.AreEqual(CardPaymentPhase.GaveUp, fixture.Session.Phase);
        Assert.IsTrue(fixture.Session.IsFinished);
        Assert.AreEqual(
            "Die Zahlung konnte nicht bestätigt werden. Bitte in der SumUp Cloud prüfen, ob die Zahlung erfolgreich war.",
            fixture.Session.StatusText);
        Assert.IsFalse(fixture.Session.CanStart);
        Assert.IsFalse(fixture.Session.CanSwitchToCash);
        Assert.IsFalse(fixture.Session.CanCancel);
        Assert.AreEqual(120, fixture.Waits.Count);
    }

    [TestMethod]
    public async Task GiveUp_UsesTheConfiguredTime()
    {
        var fixture = new CardPaymentSessionFixture(options: new ClientCardPaymentOptions { GiveUpAfter = TimeSpan.FromSeconds(6) });

        await fixture.Session.StartAsync();

        Assert.AreEqual(CardPaymentPhase.GaveUp, fixture.Session.Phase);
        Assert.AreEqual(4, fixture.Waits.Count);
    }

    [TestMethod]
    public async Task GiveUp_AlsoHappensWhileTheCloudIsUnreachable()
    {
        var fixture = new CardPaymentSessionFixture(options: new ClientCardPaymentOptions { GiveUpAfter = TimeSpan.FromSeconds(6) });
        fixture.StatusReturns(Failure("Payment.CloudUnreachable"));

        await fixture.Session.StartAsync();

        Assert.AreEqual(CardPaymentPhase.GaveUp, fixture.Session.Phase);
    }

    [TestMethod]
    public async Task GiveUp_AlsoHappensWhenTheStartWasUncertainAndNothingAnswers()
    {
        var fixture = new CardPaymentSessionFixture(options: new ClientCardPaymentOptions { GiveUpAfter = TimeSpan.FromSeconds(6) });
        fixture.StartReturns(Failure("Http.Timeout"));
        fixture.StatusReturns(Failure("Http.RequestFailed"));

        await fixture.Session.StartAsync();

        Assert.AreEqual(CardPaymentPhase.GaveUp, fixture.Session.Phase);
    }

    [TestMethod]
    public async Task GiveUp_NeverHappensForAPaymentThatEndedInTime()
    {
        var fixture = new CardPaymentSessionFixture();
        fixture.StatusReturns(
            Success(Guid.Empty, PaymentRequestStatus.Pending),
            Success(Guid.Empty, PaymentRequestStatus.Completed, null, BuildReceipt()));

        await fixture.Session.StartAsync();

        Assert.AreEqual(CardPaymentPhase.Completed, fixture.Session.Phase);
    }

    [TestMethod]
    public async Task Polling_StopsWhenTheTokenIsCancelled()
    {
        var fixture = new CardPaymentSessionFixture();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() => fixture.Session.StartAsync(cts.Token));
    }

    [TestMethod]
    public async Task PaidPayment_ShowsTheMessageAndAllowsNothing()
    {
        var fixture = new CardPaymentSessionFixture(options: new ClientCardPaymentOptions { GiveUpAfter = TimeSpan.FromSeconds(3) });
        fixture.StatusReturns(Success(Guid.Empty, PaymentRequestStatus.Paid, "Zahlung erhalten, die Bestellung wird angelegt"));
        var texts = new List<string>();
        fixture.Session.Changed += (_, _) => texts.Add(fixture.Session.StatusText);

        await fixture.Session.StartAsync();

        CollectionAssert.Contains(texts, "Zahlung erhalten, die Bestellung wird angelegt");
        Assert.IsFalse(fixture.Session.CanCancel);
        Assert.IsFalse(fixture.Session.CanSwitchToCash);
        Assert.IsFalse(fixture.Session.CanStart);
    }

    [TestMethod]
    public async Task Cancel_IsOnlyPossibleWhileThePaymentIsPending()
    {
        var fixture = new CardPaymentSessionFixture();

        await fixture.Session.CancelAsync();

        await fixture.Client.DidNotReceiveWithAnyArgs().CancelCardPaymentAsync(default, default);
        Assert.IsFalse(fixture.Session.CanCancel);
    }

    [TestMethod]
    public async Task Cancel_IsOfferedWhilePendingAndSendsTheRequestId()
    {
        var fixture = new CardPaymentSessionFixture();
        var canCancelWhilePending = false;
        fixture.Client.GetCardPaymentAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            canCancelWhilePending = fixture.Session.CanCancel;
            return Success(Guid.Empty, PaymentRequestStatus.Failed);
        });

        await fixture.Session.StartAsync();

        Assert.IsTrue(canCancelWhilePending);
    }

    [TestMethod]
    public async Task Cancel_AppliesTheStatusTheServerReturns()
    {
        var fixture = new CardPaymentSessionFixture();
        Task? running = null;
        fixture.Client.GetCardPaymentAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            running = fixture.Session.CancelAsync();
            return Success(Guid.Empty, PaymentRequestStatus.Pending, "Bitte am Kartenleser bezahlen");
        }, _ => Success(Guid.Empty, PaymentRequestStatus.Cancelled, "Die Zahlung wurde abgebrochen"));
        fixture.Client.CancelCardPaymentAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Success(Guid.Empty, PaymentRequestStatus.Pending, "Warte auf die Bestätigung von SumUp"));

        await fixture.Session.StartAsync();
        await running!;

        await fixture.Client.Received(1).CancelCardPaymentAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        Assert.AreEqual(CardPaymentPhase.Failed, fixture.Session.Phase);
    }

    [TestMethod]
    public async Task Cancel_KeepsThePaymentPendingWhenTheRequestFails()
    {
        var fixture = new CardPaymentSessionFixture();
        var texts = new List<string>();
        Task? running = null;
        fixture.Session.Changed += (_, _) => texts.Add(fixture.Session.StatusText);
        fixture.Client.GetCardPaymentAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            running ??= fixture.Session.CancelAsync();
            return Success(Guid.Empty, PaymentRequestStatus.Failed);
        });
        fixture.Client.CancelCardPaymentAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(Failure("Payment.CloudUnreachable"));

        await fixture.Session.StartAsync();
        await running!;

        CollectionAssert.Contains(texts, "Der Abbruch konnte nicht gesendet werden");
    }

    [TestMethod]
    public void Session_ReportsAmountOfTheCurrentCart()
    {
        var fixture = new CardPaymentSessionFixture(price: 42.5m);

        Assert.AreEqual(42.5m, fixture.Session.Amount);
    }

    [TestMethod]
    public async Task Changed_IsRaisedForEveryVisibleChange()
    {
        var fixture = new CardPaymentSessionFixture();
        fixture.TrackChanges();
        fixture.StatusReturns(Success(Guid.Empty, PaymentRequestStatus.Failed));

        await fixture.Session.StartAsync();

        Assert.IsGreaterThanOrEqualTo(3, fixture.Changes);
    }
}
