namespace Innkeep2.Models.Fiskaly.Tss;

public sealed record TssCredentialEntry(Guid TssId, string AdminPuk, string? AdminPin);