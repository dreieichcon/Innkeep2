namespace Innkeep2.Requests.SumUp;

public interface ISumUpConnectionChecker
{
    Task<SumUpConnectionCheckResult> CheckAccountAsync(string? apiKey, string? merchantCode, CancellationToken ct = default);

    Task<SumUpConnectionCheckResult> CheckReaderAsync(string? apiKey, string? merchantCode, string? readerId, CancellationToken ct = default);
}
