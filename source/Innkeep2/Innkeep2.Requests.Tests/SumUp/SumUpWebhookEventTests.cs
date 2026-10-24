using System.Text.Json;
using Innkeep2.Models.SumUp;

namespace Innkeep2.Requests.Tests.SumUp;

[TestClass]
public class SumUpWebhookEventTests
{
	private const string Successful = """
		{
		  "id": "evt_1",
		  "event_type": "solo.transaction.updated",
		  "payload": {
		    "client_transaction_id": "4e425463-3e1b-431d-83fa-1e51c2925e99",
		    "merchant_code": "MH4H92C7",
		    "status": "successful"
		  },
		  "timestamp": "2026-10-24T10:00:00.000Z"
		}
		""";

	private const string Failed = """
		{
		  "id": "evt_2",
		  "event_type": "solo.transaction.updated",
		  "payload": {
		    "client_transaction_id": "abc",
		    "merchant_code": "MH4H92C7",
		    "status": "failed",
		    "failure_reason": "Card declined by issuer"
		  },
		  "timestamp": "2026-10-24T10:00:05Z"
		}
		""";

	[TestMethod]
	public void Deserialize_ReadsTheDocumentedSuccessPayload()
	{
		var webhook = JsonSerializer.Deserialize<SumUpWebhookEvent>(Successful)!;

		Assert.AreEqual("evt_1", webhook.Id);
		Assert.AreEqual(SumUpWebhookEvent.SoloTransactionUpdated, webhook.EventType);
		Assert.AreEqual("4e425463-3e1b-431d-83fa-1e51c2925e99", webhook.Payload.ClientTransactionId);
		Assert.AreEqual("MH4H92C7", webhook.Payload.MerchantCode);
		Assert.AreEqual("successful", webhook.Payload.Status);
		Assert.IsNull(webhook.Payload.FailureReason);
		Assert.AreEqual(new DateTimeOffset(2026, 10, 24, 10, 0, 0, TimeSpan.Zero), webhook.Timestamp);
	}

	[TestMethod]
	public void Deserialize_ReadsTheDocumentedFailurePayloadWithReason()
	{
		var webhook = JsonSerializer.Deserialize<SumUpWebhookEvent>(Failed)!;

		Assert.AreEqual("failed", webhook.Payload.Status);
		Assert.AreEqual("Card declined by issuer", webhook.Payload.FailureReason);
	}

	[TestMethod]
	public void Deserialize_IgnoresUnknownFields()
	{
		var json = Successful.Replace("\"id\": \"evt_1\",", "\"id\": \"evt_1\", \"unknown\": {\"a\": 1},");

		var webhook = JsonSerializer.Deserialize<SumUpWebhookEvent>(json)!;

		Assert.AreEqual("evt_1", webhook.Id);
	}

	[TestMethod]
	public void Deserialize_ThrowsWithoutClientTransactionId()
	{
		var json = Successful.Replace("\"client_transaction_id\": \"4e425463-3e1b-431d-83fa-1e51c2925e99\",", string.Empty);

		Assert.ThrowsExactly<JsonException>(() => JsonSerializer.Deserialize<SumUpWebhookEvent>(json));
	}

	[TestMethod]
	public void SoloTransactionUpdated_MatchesTheDocumentedEventType()
		=> Assert.AreEqual("solo.transaction.updated", SumUpWebhookEvent.SoloTransactionUpdated);
}
