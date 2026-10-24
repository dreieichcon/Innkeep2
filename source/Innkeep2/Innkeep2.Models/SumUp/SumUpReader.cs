using System.Text.Json.Serialization;

namespace Innkeep2.Models.SumUp;

public sealed record SumUpReader
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("status")]
    public required string Status { get; init; }

    [JsonPropertyName("device")]
    public SumUpReaderDevice? Device { get; init; }
}

public sealed record SumUpReaderDevice
{
    [JsonPropertyName("identifier")]
    public string? Identifier { get; init; }

    [JsonPropertyName("model")]
    public string? Model { get; init; }
}

public sealed record SumUpReaderStatusResponse
{
    [JsonPropertyName("data")]
    public required SumUpReaderStatus Data { get; init; }
}

public sealed record SumUpReaderStatus
{
    [JsonPropertyName("status")]
    public required string Status { get; init; }

    [JsonPropertyName("state")]
    public string? State { get; init; }

    [JsonPropertyName("firmware_version")]
    public string? FirmwareVersion { get; init; }
}

public sealed record SumUpReaderList
{
    [JsonPropertyName("items")]
    public IReadOnlyList<SumUpReader> Items { get; init; } = [];
}
