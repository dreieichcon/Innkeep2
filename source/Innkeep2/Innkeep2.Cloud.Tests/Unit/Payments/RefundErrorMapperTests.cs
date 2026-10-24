using Demolite.Core.Models;
using Innkeep2.Cloud.Api;
using Innkeep2.Cloud.Services.Payments;

namespace Innkeep2.Cloud.Tests.Unit.Payments;

[TestClass]
public class RefundErrorMapperTests
{
    [TestMethod]
    public void StatusCodeFor_AnOrderThatWasRefundedAlreadyIsAConflict()
        => Assert.AreEqual(409, RefundErrorMapper.StatusCodeFor(new Error("Refund.AlreadyRefunded", "x")));

    [DataRow(CardRefundErrors.PaymentMissing)]
    [DataRow(CardRefundErrors.NotRefundable)]
    [DataRow(CardRefundErrors.Rejected)]
    [DataRow(CardRefundErrors.NotPermitted)]
    [DataRow(CardRefundErrors.Unavailable)]
    [TestMethod]
    public void StatusCodeFor_EveryCardRefundErrorIsUnprocessable(string code)
        => Assert.AreEqual(422, RefundErrorMapper.StatusCodeFor(new Error(code, "x")));

    [DataRow("Refund.PretixOrderMissing")]
    [DataRow("Refund.NoConfiguration")]
    [DataRow("Db.NotFound")]
    [DataRow("Anything.Else")]
    [TestMethod]
    public void StatusCodeFor_OtherErrorsStayBadRequests(string code)
        => Assert.AreEqual(400, RefundErrorMapper.StatusCodeFor(new Error(code, "x")));

    [TestMethod]
    public void StatusCodeFor_TheCardCodesMatchWhatTheServerPolicyReads()
    {
        Assert.IsTrue(CardRefundErrors.PaymentMissing.StartsWith("Refund."));
        Assert.IsTrue(CardRefundErrors.Unavailable.StartsWith("Refund."));
    }
}
