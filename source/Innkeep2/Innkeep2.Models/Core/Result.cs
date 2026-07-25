namespace Innkeep2.Models.Core;

public sealed class Result<T>
{
	public bool IsSuccess { get; }
	public T? Value { get; }
	public Error? Error { get; }

	private Result(T? value, bool success, Error? error)
	{
		Value = value;
		IsSuccess = success;
		Error = error;
	}

	public static Result<T> Success(T value) => new(value, true, null);

	public static Result<T> Failure(Error error) => new(default, false, error);
}