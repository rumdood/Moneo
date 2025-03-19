using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Moneo.Web.Auth.Logging;

internal class Tree
{
    public Node Root { get; set; }

    public Tree(Node root)
    {
        Root = root;
    }
    
    internal class Node
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public List<Node> Children { get; set; } = new();
    }
}

public static class ServiceCollectionExtensions
{
    private static HttpLoggingFields DefaultLoggingFields { get; } = HttpLoggingFields.RequestPath |
                                                                     HttpLoggingFields.RequestMethod |
                                                                     HttpLoggingFields.RequestHeaders |
                                                                     HttpLoggingFields.ResponseStatusCode;
    public static IServiceCollection AddMoneoHttpLogging(this IServiceCollection services)
    {
        services.AddHttpLogging(opt =>
        {
            opt.LoggingFields = DefaultLoggingFields;
            opt.RequestHeaders.Add("Content-Type");
            opt.RequestHeaders.Add("Content-Encoding");
        });

        return services;
    }
    
    public static IServiceCollection AddMoneoHttpLogging(this IServiceCollection services, IConfiguration configuration)
    {
        var requestHeaders = configuration.GetSection("Moneo:HttpLogging:RequestHeaders").Get<string>();

        services.AddHttpLogging(opt =>
        {
            opt.LoggingFields = DefaultLoggingFields;
            opt.RequestHeaders.UnionWith(requestHeaders?.Split(',').Select(x => x.Trim()) ?? []);
        });

        return services;
    }
}
