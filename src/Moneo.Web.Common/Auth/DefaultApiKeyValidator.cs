namespace Moneo.Web.Auth;

public class DefaultApiKeyValidator : IApiKeyValidator
{
    public Func<string?, Task<bool>> Validator { get; set; }
    
    public Task<bool> ValidateAsync(string? apiKey)
    {
        return Validator(apiKey);
    }
}