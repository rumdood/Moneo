using Microsoft.Extensions.DependencyInjection;

namespace Moneo.Web.Auth;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="services"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    /// <example>
    /// // using the callback approach on the default validator
    /// builder.Services.AddApiKeyAuthentication(opt =>
    /// {
    /// opt.HeaderName = "X-Api-Key";
    /// opt.UseValidationCallback(s =>
    /// {
    ///     if (s == "foo")
    ///     {
    ///       return Task.FromResult(true);
    ///     }
    /// 
    ///     return Task.FromResult(false);
    ///   });
    /// });
    ///
    /// // using a custom validator that implements IApiKeyValidator
    /// builder.Services.AddApiKeyAuthentication(opt =>
    /// {
    ///   opt.HeaderName = "X-Api-Key";
    ///   opt.RegisterKeyValidator&lt;MyApiKeyValidator&gt;();
    /// });
    /// </example>
    public static IServiceCollection AddApiKeyAuthentication(this IServiceCollection services, Action<ApiKeyAuthenticationOptions> options)
    {
        var apiKeyOptions = new ApiKeyAuthenticationOptions();
        options(apiKeyOptions);

        return services.AddApiKeyAuthentication(apiKeyOptions);
    }
    
    private static IServiceCollection AddApiKeyAuthentication(this IServiceCollection services, ApiKeyAuthenticationOptions options)
    {
        if (options is { UseDefaultValidator: false, ValidatorType: null })
        {
            throw new InvalidOperationException("ApiKeyValidatorType or Callback must be set");
        }
        
        if (string.IsNullOrEmpty(options.HeaderName))
        {
            throw new InvalidOperationException("Api Key HeaderName must be set");
        }

        if (options.UseDefaultValidator)
        {
            services.AddSingleton<IApiKeyValidator>(_ => new DefaultApiKeyValidator
                { Validator = options.ValidateCallback! });
        }
        else
        {
            services.AddSingleton(typeof(IApiKeyValidator), options.ValidatorType!);
        }
        
        services.AddAuthentication(ApiKeyAuthenticationDefaults.ApiKeyAuthenticationScheme)
            .AddScheme<ApiKeyAuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
                ApiKeyAuthenticationDefaults.ApiKeyAuthenticationScheme,
                opt =>
                {
                    opt.HeaderName = options.HeaderName;
                });
        
        return services;
    }
}

public class ApiKeyAuthenticationOptions
{
    public string HeaderName { get; set; } = "X-Api-Key";
    public Type? ValidatorType { get; private set; }
    public Func<string?, Task<bool>>? ValidateCallback { get; private set; }
    public bool UseDefaultValidator => ValidatorType == null && ValidateCallback != null;
    
    public void RegisterKeyValidator<TValidator>()
        where TValidator : class, IApiKeyValidator
    {
        ValidatorType = typeof(TValidator);
    }
    
    public void UseValidationCallback(Func<string?, Task<bool>> validateCallback)
    {
        ValidateCallback = validateCallback;
    }
}