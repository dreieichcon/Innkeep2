using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Innkeep2.Models.Fiskaly.Tss;

[UsedImplicitly]
public sealed record FiskalyTss
{
    [JsonPropertyName("_id")]
    public required Guid Id { get; init; }

    [JsonPropertyName("_env")]
    public required string Environment { get; init; }

    [JsonPropertyName("state")]
    public required TssState State { get; init; }

    [JsonPropertyName("admin_puk")]
    public string? AdminPuk { get; init; }

    [JsonPropertyName("certificate")]
    public required string Certificate { get; init; }
    
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("serial_number")]
    public required string SerialNumber { get; init; }

    [JsonPropertyName("public_key")]
    public required string PublicKey { get; init; }

    [JsonPropertyName("max_number_registered_clients")]
    public required long MaxNumberRegisteredClients { get; init; }

    [JsonPropertyName("max_number_active_transactions")]
    public required long MaxNumberActiveTransactions { get; init; }

    [JsonPropertyName("time_creation")]
    public required long TimeCreation { get; init; }

    [JsonIgnore]
    public string DisplayName => $"{Description} ({Id})";
}