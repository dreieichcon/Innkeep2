using Innkeep2.Cloud.TransactionDb.Models;
using Innkeep2.Cloud.TransactionDb.Repositories;
using Innkeep2.Models.Shared;

namespace Innkeep2.Cloud.Tests.Integration.Payments;

[TestClass]
public class PaymentRequestRepositoryTests
{
    private static readonly DateTime Now = new(2026, 10, 24, 12, 0, 0, DateTimeKind.Utc);

    private SqliteTransactionDbContextFactory _factory = null!;
    private PaymentRequestRepository _repository = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _factory = new SqliteTransactionDbContextFactory();
        _repository = new PaymentRequestRepository(_factory);
    }

    [TestCleanup]
    public void TestCleanup()
        => _factory.Dispose();

    private static PaymentRequest BuildRequest(Guid? requestId = null, PaymentRequestStatus status = PaymentRequestStatus.Pending, DateTime? createdAt = null, string? clientTransactionId = null) => new()
    {
        RequestId = requestId ?? Guid.NewGuid(),
        OrderRequestJson = """{"requestId":"x"}""",
        TotalAmount = 15.5m,
        Currency = "EUR",
        Status = status,
        ClientTransactionId = clientTransactionId,
        CreatedAt = createdAt ?? Now,
        UpdatedAt = createdAt ?? Now
    };

    [TestMethod]
    public async Task Create_ThenGetByRequestId_ReturnsStoredValues()
    {
        var requestId = Guid.NewGuid();
        var request = BuildRequest(requestId);
        request.CheckoutId = "chk_1";
        request.FailureReason = "reason";
        request.ClientTransactionId = "4e425463-3e1b-431d-83fa-1e51c2925e99";

        var created = await _repository.CreateAsync(request);
        var loaded = await _repository.GetByRequestIdAsync(requestId);

        Assert.IsTrue(created.IsSuccess);
        Assert.IsTrue(loaded.IsSuccess);
        Assert.AreEqual(requestId, loaded.Value!.RequestId);
        Assert.AreEqual("""{"requestId":"x"}""", loaded.Value.OrderRequestJson);
        Assert.AreEqual(15.5m, loaded.Value.TotalAmount);
        Assert.AreEqual("EUR", loaded.Value.Currency);
        Assert.AreEqual(PaymentRequestStatus.Pending, loaded.Value.Status);
        Assert.AreEqual("4e425463-3e1b-431d-83fa-1e51c2925e99", loaded.Value.ClientTransactionId);
        Assert.AreEqual("chk_1", loaded.Value.CheckoutId);
        Assert.AreEqual("reason", loaded.Value.FailureReason);
        Assert.AreEqual(Now, loaded.Value.CreatedAt);
        Assert.AreEqual(Now, loaded.Value.UpdatedAt);
    }

    [TestMethod]
    public void NewRequest_DefaultsToPending()
    {
        var request = new PaymentRequest
        {
            RequestId = Guid.NewGuid(),
            OrderRequestJson = "{}",
            TotalAmount = 1m,
            Currency = "EUR",
            CreatedAt = Now,
            UpdatedAt = Now
        };

        Assert.AreEqual(PaymentRequestStatus.Pending, request.Status);
    }

    [TestMethod]
    public async Task GetByRequestId_ReturnsFailureForUnknownId()
    {
        await _repository.CreateAsync(BuildRequest());

        var result = await _repository.GetByRequestIdAsync(Guid.NewGuid());

        Assert.IsFalse(result.IsSuccess);
    }

    [TestMethod]
    public async Task GetByRequestId_ReturnsTheMatchingRequestOfSeveral()
    {
        var wanted = Guid.NewGuid();
        await _repository.CreateAsync(BuildRequest());
        await _repository.CreateAsync(BuildRequest(wanted));
        await _repository.CreateAsync(BuildRequest());

        var result = await _repository.GetByRequestIdAsync(wanted);

        Assert.AreEqual(wanted, result.Value!.RequestId);
    }

    [TestMethod]
    public async Task GetByClientTransactionId_ReturnsMatchingRequest()
    {
        var wanted = Guid.NewGuid();
        await _repository.CreateAsync(BuildRequest(clientTransactionId: "other"));
        await _repository.CreateAsync(BuildRequest(wanted, clientTransactionId: "abc"));

        var result = await _repository.GetByClientTransactionIdAsync("abc");

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(wanted, result.Value!.RequestId);
    }

    [TestMethod]
    public async Task GetByClientTransactionId_ReturnsFailureForUnknownId()
    {
        await _repository.CreateAsync(BuildRequest(clientTransactionId: "abc"));

        var result = await _repository.GetByClientTransactionIdAsync("unknown");

        Assert.IsFalse(result.IsSuccess);
    }

    [TestMethod]
    public async Task GetByClientTransactionId_DoesNotMatchRequestsWithoutOne()
    {
        await _repository.CreateAsync(BuildRequest());

        var result = await _repository.GetByClientTransactionIdAsync("abc");

        Assert.IsFalse(result.IsSuccess);
    }

    [TestMethod]
    public async Task Create_RejectsDuplicateRequestId()
    {
        var requestId = Guid.NewGuid();
        await _repository.CreateAsync(BuildRequest(requestId));

        var duplicate = await _repository.CreateAsync(BuildRequest(requestId));

        Assert.IsFalse(duplicate.IsSuccess);
        var all = await _repository.GetAllAsync();
        Assert.HasCount(1, all.Value!);
    }

    [TestMethod]
    public async Task Update_PersistsStatusAndTransactionData()
    {
        var requestId = Guid.NewGuid();
        var created = await _repository.CreateAsync(BuildRequest(requestId));
        var entity = created.Value!;

        entity.Status = PaymentRequestStatus.Paid;
        entity.ClientTransactionId = "abc";
        entity.UpdatedAt = Now.AddSeconds(30);
        var updated = await _repository.UpdateAsync(entity);
        var loaded = await _repository.GetByRequestIdAsync(requestId);

        Assert.IsTrue(updated.IsSuccess);
        Assert.AreEqual(PaymentRequestStatus.Paid, loaded.Value!.Status);
        Assert.AreEqual("abc", loaded.Value.ClientTransactionId);
        Assert.AreEqual(Now.AddSeconds(30), loaded.Value.UpdatedAt);
    }

    [TestMethod]
    public async Task HasPending_ReturnsFalseWithoutRequests()
    {
        var result = await _repository.HasPendingAsync(Now.AddMinutes(-3));

        Assert.IsTrue(result.IsSuccess);
        Assert.IsFalse(result.Value);
    }

    [TestMethod]
    public async Task HasPending_ReturnsTrueForRecentPendingRequest()
    {
        await _repository.CreateAsync(BuildRequest(createdAt: Now.AddSeconds(-30)));

        var result = await _repository.HasPendingAsync(Now.AddMinutes(-3));

        Assert.IsTrue(result.Value);
    }

    [TestMethod]
    public async Task HasPending_ReturnsFalseForPendingRequestOlderThanCutoff()
    {
        await _repository.CreateAsync(BuildRequest(createdAt: Now.AddMinutes(-10)));

        var result = await _repository.HasPendingAsync(Now.AddMinutes(-3));

        Assert.IsFalse(result.Value);
    }

    [TestMethod]
    public async Task HasPending_CountsRequestCreatedExactlyAtCutoff()
    {
        var cutoff = Now.AddMinutes(-3);
        await _repository.CreateAsync(BuildRequest(createdAt: cutoff));

        var result = await _repository.HasPendingAsync(cutoff);

        Assert.IsTrue(result.Value);
    }

    [DataRow(PaymentRequestStatus.Paid)]
    [DataRow(PaymentRequestStatus.Completed)]
    [DataRow(PaymentRequestStatus.Failed)]
    [DataRow(PaymentRequestStatus.Cancelled)]
    [DataRow(PaymentRequestStatus.Expired)]
    [TestMethod]
    public async Task HasPending_IgnoresRequestsThatAreNotPending(PaymentRequestStatus status)
    {
        await _repository.CreateAsync(BuildRequest(status: status, createdAt: Now.AddSeconds(-10)));

        var result = await _repository.HasPendingAsync(Now.AddMinutes(-3));

        Assert.IsFalse(result.Value);
    }

    [TestMethod]
    public async Task HasPending_ReturnsFalseOnceTheRequestLeavesPending()
    {
        var created = await _repository.CreateAsync(BuildRequest(createdAt: Now.AddSeconds(-10)));
        created.Value!.Status = PaymentRequestStatus.Completed;
        await _repository.UpdateAsync(created.Value);

        var result = await _repository.HasPendingAsync(Now.AddMinutes(-3));

        Assert.IsFalse(result.Value);
    }

    [TestMethod]
    public async Task Create_StoresSumupTransactionCodeAndTerminationReason()
    {
        var requestId = Guid.NewGuid();
        var request = BuildRequest(requestId);
        request.SumupTransactionCode = "TEENSK4W2K";
        request.TerminationReason = PaymentTerminationReason.UserCancelled;

        await _repository.CreateAsync(request);
        var loaded = await _repository.GetByRequestIdAsync(requestId);

        Assert.AreEqual("TEENSK4W2K", loaded.Value!.SumupTransactionCode);
        Assert.AreEqual(PaymentTerminationReason.UserCancelled, loaded.Value.TerminationReason);
    }

    [TestMethod]
    public async Task Create_LeavesSumupTransactionCodeAndTerminationReasonEmptyByDefault()
    {
        var requestId = Guid.NewGuid();

        await _repository.CreateAsync(BuildRequest(requestId));
        var loaded = await _repository.GetByRequestIdAsync(requestId);

        Assert.IsNull(loaded.Value!.SumupTransactionCode);
        Assert.IsNull(loaded.Value.TerminationReason);
    }

    [TestMethod]
    public async Task Transaction_StoresSumupTransactionCodeForCardPayments()
    {
        var transactions = new TransactionRepository(_factory);
        var requestId = Guid.NewGuid();

        await transactions.CreateAsync(BuildTransaction(requestId, PaymentType.NonCash, "TEENSK4W2K"));
        var loaded = await transactions.GetCustomAsync(x => x.RequestId == requestId);

        Assert.AreEqual("TEENSK4W2K", loaded.Value!.SumupTransactionCode);
        Assert.AreEqual(PaymentType.NonCash, loaded.Value.PaymentType);
    }

    [TestMethod]
    public async Task Transaction_AllowsCashPaymentsWithoutSumupTransactionCode()
    {
        var transactions = new TransactionRepository(_factory);
        var requestId = Guid.NewGuid();

        await transactions.CreateAsync(BuildTransaction(requestId, PaymentType.Cash, null));
        var loaded = await transactions.GetCustomAsync(x => x.RequestId == requestId);

        Assert.IsTrue(loaded.IsSuccess);
        Assert.IsNull(loaded.Value!.SumupTransactionCode);
    }

    private static Transaction BuildTransaction(Guid requestId, PaymentType paymentType, string? sumupTransactionCode) => new()
    {
        RequestId = requestId,
        Title = "Title",
        Header = "Header",
        TransactionType = TransactionType.Sale,
        BookingTime = Now,
        PaymentType = paymentType,
        TotalAmount = 10m,
        AmountGiven = 10m,
        AmountBack = 0m,
        Currency = "EUR",
        RequestJson = "{}",
        SumupTransactionCode = sumupTransactionCode
    };
}
