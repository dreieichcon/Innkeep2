using Innkeep2.Models.Core;
using Innkeep2.Requests.Core;
using MudBlazor;

namespace Innkeep2.Cloud.Services;

public sealed class UiResultHandler(ISnackbar snackbar)
{
	public async Task<T?> TryExecuteAsync<T>(Func<Task<Result<T>>> operation, string? errorPrefix = null)
	{
		var result = await operation();

		if (result.IsSuccess)
			return result.Value;

		snackbar.Add(FormatError(result.Error!, errorPrefix), Severity.Error);
		return default;
	}

	public async Task<T> TryExecuteAsync<T>(Func<Task<Result<T>>> operation, T fallback, string? errorPrefix = null)
	{
		var result = await operation();

		if (result.IsSuccess)
			return result.Value!;

		snackbar.Add(FormatError(result.Error!, errorPrefix), Severity.Error);
		return fallback;
	}

	public async Task<bool> TryExecuteAsync(Func<Task<Result<Unit>>> operation, string? errorPrefix = null)
	{
		var result = await operation();

		if (result.IsSuccess)
			return true;

		snackbar.Add(FormatError(result.Error!, errorPrefix), Severity.Error);
		return false;
	}
	
	public async Task<T?> TryExecuteAsync<T>(
		Func<Task<Result<T>>> operation,
		string? successMessage = null,
		string? errorPrefix = null)
	{
		var result = await operation();

		if (result.IsSuccess)
		{
			if (successMessage is not null)
				snackbar.Add(successMessage, Severity.Success);

			return result.Value;
		}

		snackbar.Add(FormatError(result.Error!, errorPrefix), Severity.Error);
		return default;
	}

	private static string FormatError(Error error, string? prefix) =>
		string.IsNullOrEmpty(prefix) ? error.Message : $"{prefix}: {error.Message}";
}