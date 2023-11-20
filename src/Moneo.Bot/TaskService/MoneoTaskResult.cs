namespace Moneo.Bot;

public record MoneoTaskResult(bool IsSuccessful, string? ErrorMessage = null);

public record MoneoTaskResult<T>(bool IsSuccessful, T Result, string? ErrorMessage = null);
