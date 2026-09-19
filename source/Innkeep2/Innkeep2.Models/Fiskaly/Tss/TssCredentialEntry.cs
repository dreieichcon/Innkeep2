namespace Innkeep2.Models.Fiskaly.Tss;

public sealed record TssCredentialEntry
{
    public required Guid TssId { get; set; }
    public required string AdminPuk { get; set; }
    public string? AdminPin { get; set; }
}