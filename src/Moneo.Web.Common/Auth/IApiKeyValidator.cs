namespace Moneo.Web.Auth;

public interface IApiKeyValidator
{
    Task<bool> ValidateAsync(string? apiKey);
}