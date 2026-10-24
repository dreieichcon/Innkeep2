using System.Text.RegularExpressions;
using Demolite.Core.Models;
using Innkeep2.CredentialsDb;
using Innkeep2.CredentialsDb.Interfaces;
using Innkeep2.CredentialsDb.Models;
using Innkeep2.Models.SumUp;
using Innkeep2.Requests.SumUp;

namespace Innkeep2.Cloud.Services.Payments;

public enum SumUpPairingState
{
    Paired,
    InvalidInput,
    NoAccountCredential,
    InvalidKey,
    InvalidCode,
    CodeNotPending,
    Expired,
    TimedOut,
    Unreachable,
    SaveFailed
}

public sealed record SumUpPairingResult(SumUpPairingState State, string Message, SumUpReaderCredential? Reader = null)
{
    public bool IsPaired => State == SumUpPairingState.Paired;
}

public sealed partial class SumUpReaderService(
    ISumUpClient client,
    ActiveCredentialProvider<SumUpCredential> accountCredentials,
    ICredentialRepository<SumUpReaderCredential> readerRepository,
    ActiveCredentialProvider<SumUpReaderCredential> readerCredentials,
    SumUpPairingOptions options)
{
    private const string PairedStatus = "paired";
    private const string ExpiredStatus = "expired";
    private const string NoAccountMessage = "Bitte zuerst SumUp-Zugangsdaten anlegen und aktivieren";

    [GeneratedRegex("^[A-Za-z0-9]{8,9}$")]
    private static partial Regex PairingCodePattern();

    /// <summary>
    /// Pairs a reader with the active SumUp account and waits until the device confirms the pairing.
    /// </summary>
    public async Task<SumUpPairingResult> PairAsync(string? pairingCode, string? readerName, CancellationToken ct = default)
    {
        var code = pairingCode?.Trim() ?? string.Empty;
        var name = readerName?.Trim() ?? string.Empty;

        return (code, name, accountCredentials.Active) switch
        {
            _ when !PairingCodePattern().IsMatch(code)
                => Fail(SumUpPairingState.InvalidInput, "Bitte den 8- oder 9-stelligen Kopplungscode vom Reader eingeben"),
            _ when name.Length == 0
                => Fail(SumUpPairingState.InvalidInput, "Bitte einen Namen für den Reader angeben"),
            (_, _, null)
                => Fail(SumUpPairingState.NoAccountCredential, NoAccountMessage),
            (_, _, { } account)
                => await CreateAndAwaitAsync(account, code, name, ct)
        };
    }

    /// <summary>
    /// Lists the paired readers of the account that are not saved yet.
    /// </summary>
    public async Task<Result<IReadOnlyList<SumUpReader>>> GetAdoptableReadersAsync(CancellationToken ct = default)
    {
        if (accountCredentials.Active is not { } account)
            return Result<IReadOnlyList<SumUpReader>>.Failure(new Error("SumUp.NoAccount", NoAccountMessage));

        var listed = await client.ListReadersAsync(account.MerchantCode, ct);

        if (!listed.IsSuccess)
            return Result<IReadOnlyList<SumUpReader>>.Failure(DescribeError(listed.Error!));

        var saved = await readerRepository.GetAllAsync();

        if (!saved.IsSuccess)
            return Result<IReadOnlyList<SumUpReader>>.Failure(saved.Error!);

        var knownIds = saved.Value!.Select(x => x.ReaderId).ToHashSet();
        IReadOnlyList<SumUpReader> adoptable = listed.Value!.Items
            .Where(x => IsStatus(x, PairedStatus) && !knownIds.Contains(x.Id))
            .ToList();

        return Result<IReadOnlyList<SumUpReader>>.Success(adoptable);
    }

    /// <summary>
    /// Saves a reader that is already paired at SumUp as a reader credential.
    /// </summary>
    public async Task<Result<SumUpReaderCredential>> AdoptReaderAsync(SumUpReader reader, string? readerName, CancellationToken ct = default)
    {
        if (!IsStatus(reader, PairedStatus))
            return Result<SumUpReaderCredential>.Failure(new Error("SumUp.NotPaired", "Der Reader ist bei SumUp nicht gekoppelt"));

        if (accountCredentials.Active is not { } account)
            return Result<SumUpReaderCredential>.Failure(new Error("SumUp.NoAccount", NoAccountMessage));

        var name = string.IsNullOrWhiteSpace(readerName) ? reader.Name ?? reader.Id : readerName.Trim();
        var saved = await SaveReaderAsync(account.MerchantCode, reader, name);

        return saved.IsPaired
            ? Result<SumUpReaderCredential>.Success(saved.Reader!)
            : Result<SumUpReaderCredential>.Failure(new Error("SumUp.SaveFailed", saved.Message));
    }

    /// <summary>
    /// Deletes the reader at SumUp and then removes it locally. A reader SumUp does not know is removed locally anyway.
    /// </summary>
    public async Task<Result> UnpairAsync(SumUpReaderCredential reader, CancellationToken ct = default)
    {
        if (!accountCredentials.HasActive)
            return Result.Failure(new Error("SumUp.NoAccount", NoAccountMessage));

        var deleted = await client.DeleteReaderAsync(reader.MerchantCode, reader.ReaderId, ct);

        if (!deleted.IsSuccess && deleted.Error!.Code != "Http.404")
            return Result.Failure(DescribeError(deleted.Error));

        var removed = await readerRepository.DeleteAsync(reader.Id);

        if (!removed.IsSuccess)
            return Result.Failure(removed.Error!);

        await readerCredentials.ReloadAsync();

        return Result.Success();
    }

    private async Task<SumUpPairingResult> CreateAndAwaitAsync(SumUpCredential account, string code, string name, CancellationToken ct)
    {
        var created = await client.CreateReaderAsync(account.MerchantCode, new SumUpCreateReaderRequest { PairingCode = code, Name = name }, ct);

        return created.IsSuccess
            ? await AwaitPairingAsync(account.MerchantCode, created.Value!, name, ct)
            : MapCreateError(created.Error!);
    }

    private async Task<SumUpPairingResult> AwaitPairingAsync(string merchantCode, SumUpReader reader, string name, CancellationToken ct)
    {
        var current = reader;

        for (var poll = 0; ; poll++)
        {
            if (IsStatus(current, PairedStatus))
                return await SaveReaderAsync(merchantCode, current, name);

            if (IsStatus(current, ExpiredStatus))
                return Fail(SumUpPairingState.Expired, "Der Kopplungscode ist abgelaufen, bitte die Kopplung am Reader neu starten");

            if (poll >= options.MaxPolls)
                return Fail(SumUpPairingState.TimedOut, "Die Kopplung wurde am Reader nicht bestätigt, bitte erneut versuchen");

            await Task.Delay(options.PollInterval, ct);

            var polled = await client.GetReaderAsync(merchantCode, reader.Id, ct);

            switch (polled.Error?.Code)
            {
                case null:
                    current = polled.Value!;
                    break;
                case "Http.401" or "Http.403":
                    return Fail(SumUpPairingState.InvalidKey, "API Key ungültig oder Berechtigung readers.read fehlt");
                case "Http.404":
                    return Fail(SumUpPairingState.Unreachable, "Der Reader ist bei SumUp nicht mehr vorhanden, bitte erneut versuchen");
            }
        }
    }

    private async Task<SumUpPairingResult> SaveReaderAsync(string merchantCode, SumUpReader reader, string name)
    {
        var existing = await readerRepository.GetAllAsync();

        if (existing.IsSuccess && existing.Value!.FirstOrDefault(x => x.ReaderId == reader.Id) is { } known)
            return new SumUpPairingResult(SumUpPairingState.Paired, $"Reader '{known.Name}' ist bereits gespeichert", known);

        var credential = new SumUpReaderCredential
        {
            Name = name,
            ReaderId = reader.Id,
            MerchantCode = merchantCode,
            DeviceModel = reader.Device?.Model,
            DeviceIdentifier = reader.Device?.Identifier
        };

        var saved = await readerRepository.CreateAsync(credential);

        if (!saved.IsSuccess)
            return Fail(SumUpPairingState.SaveFailed, "Der Reader ist gekoppelt, konnte aber nicht gespeichert werden");

        await readerCredentials.ReloadAsync();

        return new SumUpPairingResult(SumUpPairingState.Paired, $"Reader '{name}' wurde gekoppelt", saved.Value);
    }

    private static SumUpPairingResult MapCreateError(Error error)
        => error.Code switch
        {
            "Http.404" => Fail(SumUpPairingState.InvalidCode, "Kopplungscode unbekannt oder abgelaufen, bitte am Reader einen neuen Code erzeugen"),
            "Http.409" => Fail(SumUpPairingState.CodeNotPending, "Der Reader wartet nicht auf eine Kopplung, bitte die Kopplung am Reader neu starten"),
            "Http.401" or "Http.403" => Fail(SumUpPairingState.InvalidKey, "API Key ungültig oder Berechtigung readers.write fehlt"),
            "Http.400" => Fail(SumUpPairingState.InvalidInput, "SumUp hat die Eingabe abgelehnt, bitte Code und Namen prüfen"),
            _ => Fail(SumUpPairingState.Unreachable, "SumUp nicht erreichbar")
        };

    private static Error DescribeError(Error error)
        => error.Code switch
        {
            "Http.401" or "Http.403" => new Error(error.Code, "API Key ungültig oder Berechtigung fehlt"),
            "Http.404" => new Error(error.Code, "Händlercode nicht gefunden"),
            _ => new Error(error.Code, "SumUp nicht erreichbar")
        };

    private static bool IsStatus(SumUpReader reader, string status)
        => string.Equals(reader.Status, status, StringComparison.OrdinalIgnoreCase);

    private static SumUpPairingResult Fail(SumUpPairingState state, string message)
        => new(state, message);
}
