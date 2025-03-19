using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Moneo.Web.Auth;

public class ApiKeyAuthenticationDefaults
{
    public const string ApiKeyAuthenticationScheme = "ApiKey";
}

public class ApiKeyAuthenticationSchemeOptions: AuthenticationSchemeOptions
{
    public string HeaderName {get; set;} = "X-Api-Key";
}

public class ApiKeyAuthenticationHandler: AuthenticationHandler<ApiKeyAuthenticationSchemeOptions>
{
    private readonly IApiKeyValidator _apiKeyValidator;
    
    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IApiKeyValidator apiKeyValidator) : base(options, logger, encoder)
    {
        _apiKeyValidator = apiKeyValidator;
    }
    
    private AuthenticationTicket GetAuthenticationTicket(string userName)
    {
        var claims = new[] { new Claim(ClaimTypes.Name, userName) };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        return new AuthenticationTicket(principal, Scheme.Name);
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var endpoint = Context.GetEndpoint();

        if (endpoint?.Metadata.GetMetadata<IAuthorizeData>() == null)
        {
            // no authorization is needed for this endpoint, don't check the API key
            var anonymousTicket = GetAuthenticationTicket("Anonymous User");
            return AuthenticateResult.Success(anonymousTicket);
        }
        
        if (!Request.Headers.TryGetValue(Options.HeaderName, out var apiKeyHeader)) {
            return AuthenticateResult.Fail("Missing API key");
        }

        var apiKey = apiKeyHeader.FirstOrDefault();
        var isValid = await _apiKeyValidator.ValidateAsync(apiKey);

        if (!isValid) {
            return AuthenticateResult.Fail("Invalid API key");
        }

        var ticket = GetAuthenticationTicket("ApiKey User");
        return AuthenticateResult.Success(ticket);
    }
}