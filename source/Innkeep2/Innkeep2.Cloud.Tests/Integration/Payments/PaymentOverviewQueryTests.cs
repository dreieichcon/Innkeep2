using System.Text.Json;
using Innkeep2.Cloud.TransactionDb.Models;
using Innkeep2.Cloud.TransactionDb.Repositories;
using Innkeep2.Models.Shared;

namespace Innkeep2.Cloud.Tests.Integration.Payments;

[TestClass]
public class PaymentOverviewQueryTests
{
    private static readonly DateTime Now = new(2026, 10, 24, 12, 0, 0, DateTimeKind.Utc);

    private SqliteTransactionDbContextFactory _factory = null!;
    private PaymentRequestRepository _payments = null!;
    private TransactionRepository _transactions = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _factory = new SqliteTransactionDbContextFactory();
        _payments = new PaymentRequestRepository(_factory);
        _transactions = new TransactionRepository(_factory);
    }

    [TestCleanup]
    public void TestCleanup()
        => _factory.Dispose();

    private async Task<Guid> AddPaymentAsync(PaymentRequestStatus status = PaymentRequestStatus.Completed, DateTime? createdAt = null)
    {
        var payment = new PaymentRequest
        {
            RequestId = Guid.NewGuid(),
            OrderRequestJson = "{}",
            TotalAmount = 10m,
            Currency = "EUR",
            MerchantCode = "MH4H92C7",
            ReaderId = "rdr_1",
            Status = status,
            CreatedAt = createdAt ?? Now,
            UpdatedAt = createdAt ?? Now
        };

        await _payments.CreateAsync(payment);

        return payment.RequestId;
    }

    private async Task AddTransactionAsync(Guid requestId, TransactionType type = TransactionType.Sale, TransactionStepStatus pretix = TransactionStepStatus.Completed, TransactionStepStatus fiskaly = TransactionStepStatus.Completed)
        => await _transactions.CreateAsync(new Transaction
        {
            RequestId = requestId,
            Title = "Title",
            Header = "Header",
            TransactionType = type,
            BookingTime = Now,
            PaymentType = PaymentType.NonCash,
            TotalAmount = 10m,
            AmountGiven = 10m,
            AmountBack = 0m,
            Currency = "EUR",
            RequestJson = JsonSerializer.Serialize(new { }),
            PretixStatus = pretix,
            FiskalyStatus = fiskaly
        });

    private Task<Demolite.Core.Models.Result<Innkeep2.Models.Core.PagedResult<PaymentOverviewRow>>> QueryAsync(
        int skip = 0,
        int take = 50,
        PaymentRequestStatus? status = null,
        bool onlyNeedingAttention = false,
        DateTime? hangingBefore = null)
        => _payments.GetOverviewAsync(skip, take, status, onlyNeedingAttention, hangingBefore ?? Now.AddMinutes(-3));

    [TestMethod]
    public async Task Overview_IsEmptyWithoutPayments()
    {
        var result = await QueryAsync();

        Assert.IsTrue(result.IsSuccess);
        Assert.IsEmpty(result.Value!.Items);
        Assert.AreEqual(0, result.Value.TotalCount);
    }

    [TestMethod]
    public async Task Overview_ListsTheNewestPaymentFirst()
    {
        var oldest = await AddPaymentAsync(createdAt: Now.AddHours(-3));
        var newest = await AddPaymentAsync(createdAt: Now);
        var middle = await AddPaymentAsync(createdAt: Now.AddHours(-1));

        var result = await QueryAsync();

        CollectionAssert.AreEqual(new[] { newest, middle, oldest }, result.Value!.Items.Select(x => x.Payment.RequestId).ToArray());
    }

    [TestMethod]
    public async Task Overview_PagesWithSkipAndTakeAndReportsTheTotal()
    {
        for (var i = 0; i < 7; i++)
            await AddPaymentAsync(createdAt: Now.AddMinutes(-i));

        var page = await QueryAsync(skip: 3, take: 3);
        var last = await QueryAsync(skip: 6, take: 3);

        Assert.HasCount(3, page.Value!.Items);
        Assert.AreEqual(7, page.Value.TotalCount);
        Assert.HasCount(1, last.Value!.Items);
    }

    [TestMethod]
    public async Task Overview_PagesDoNotOverlap()
    {
        for (var i = 0; i < 5; i++)
            await AddPaymentAsync(createdAt: Now.AddMinutes(-i));

        var first = await QueryAsync(skip: 0, take: 3);
        var second = await QueryAsync(skip: 3, take: 3);

        var ids = first.Value!.Items.Concat(second.Value!.Items).Select(x => x.Payment.RequestId).ToList();
        Assert.HasCount(5, ids.Distinct().ToList());
    }

    [TestMethod]
    public async Task Overview_FiltersByStatusAndCountsOnlyThose()
    {
        await AddPaymentAsync(PaymentRequestStatus.Failed);
        await AddPaymentAsync(PaymentRequestStatus.Failed);
        await AddPaymentAsync(PaymentRequestStatus.Completed);

        var result = await QueryAsync(status: PaymentRequestStatus.Failed);

        Assert.AreEqual(2, result.Value!.TotalCount);
        Assert.IsTrue(result.Value.Items.All(x => x.Payment.Status == PaymentRequestStatus.Failed));
    }

    [TestMethod]
    public async Task Overview_ShowsWhetherAnOrderExistsWithItsStepStatuses()
    {
        var withOrder = await AddPaymentAsync();
        var withoutOrder = await AddPaymentAsync(createdAt: Now.AddMinutes(-1));
        await AddTransactionAsync(withOrder, pretix: TransactionStepStatus.Failed, fiskaly: TransactionStepStatus.Completed);

        var rows = (await QueryAsync()).Value!.Items;

        var first = rows.Single(x => x.Payment.RequestId == withOrder);
        Assert.IsTrue(first.HasOrder);
        Assert.AreEqual(TransactionStepStatus.Failed, first.PretixStatus);
        Assert.AreEqual(TransactionStepStatus.Completed, first.FiskalyStatus);
        var second = rows.Single(x => x.Payment.RequestId == withoutOrder);
        Assert.IsFalse(second.HasOrder);
        Assert.IsNull(second.PretixStatus);
        Assert.IsNull(second.FiskalyStatus);
    }

    [DataRow(TransactionType.Refund)]
    [DataRow(TransactionType.Transfer)]
    [TestMethod]
    public async Task Overview_OnlyASaleCountsAsTheOrder(TransactionType type)
    {
        var requestId = await AddPaymentAsync();
        await AddTransactionAsync(requestId, type);

        var row = (await QueryAsync()).Value!.Items.Single();

        Assert.IsFalse(row.HasOrder);
    }

    [TestMethod]
    public async Task Overview_OnlyNeedingAttention_ShowsHangingPaidAndCompletedWithoutOrder()
    {
        var hanging = await AddPaymentAsync(PaymentRequestStatus.Pending, Now.AddMinutes(-10));
        var paid = await AddPaymentAsync(PaymentRequestStatus.Paid);
        var completedNoOrder = await AddPaymentAsync();
        var recentPending = await AddPaymentAsync(PaymentRequestStatus.Pending, Now.AddSeconds(-30));
        var completedWithOrder = await AddPaymentAsync();
        await AddTransactionAsync(completedWithOrder);
        await AddPaymentAsync(PaymentRequestStatus.Failed);
        await AddPaymentAsync(PaymentRequestStatus.Expired);

        var result = await QueryAsync(onlyNeedingAttention: true);

        var ids = result.Value!.Items.Select(x => x.Payment.RequestId).ToList();
        Assert.AreEqual(3, result.Value.TotalCount);
        CollectionAssert.AreEquivalent(new[] { hanging, paid, completedNoOrder }, ids);
        CollectionAssert.DoesNotContain(ids, recentPending);
        CollectionAssert.DoesNotContain(ids, completedWithOrder);
    }

    [TestMethod]
    public async Task Overview_OnlyNeedingAttention_TreatsAPendingPaymentExactlyAtTheCutoffAsHanging()
    {
        var cutoff = Now.AddMinutes(-3);
        await AddPaymentAsync(PaymentRequestStatus.Pending, cutoff);

        var result = await QueryAsync(onlyNeedingAttention: true, hangingBefore: cutoff);

        Assert.AreEqual(1, result.Value!.TotalCount);
    }

    [TestMethod]
    public async Task Overview_CombinesTheStatusAndTheAttentionFilter()
    {
        await AddPaymentAsync(PaymentRequestStatus.Paid);
        await AddPaymentAsync();

        var result = await QueryAsync(status: PaymentRequestStatus.Completed, onlyNeedingAttention: true);

        Assert.AreEqual(1, result.Value!.TotalCount);
        Assert.AreEqual(PaymentRequestStatus.Completed, result.Value.Items.Single().Payment.Status);
    }
}
