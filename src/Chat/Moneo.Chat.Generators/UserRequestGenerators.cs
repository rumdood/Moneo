using System.Collections.Immutable;
using System.Text;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Moneo.Chat.Generators;

[Generator]
public class UserRequestGenerators : IIncrementalGenerator
{
    private static class AttributeKeys
    {
        public const string CommandKey = "CommandKey";
        public const string UserCommandAttribute = "UserCommandAttribute";
        public const string UserCommandArgumentAttribute = "UserCommandArgument";
        public const string UserCommandHelpText = "HelpDescription";
    }

    private record struct UserRequestInfo
    {
        public string Namespace { get; set; }
        public string Name { get; set; }
        public string CommandKey { get; set; }
        public string HelpText { get; set; }
    }

    private record struct UserCommandAttributeData
    {
        public string UserCommand { get; set; }
        public string? HelpText { get; set; }
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Collect user requests from the current project
        var requestsProvider = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsClassDeclarationSyntaxWithBaseList(s),
                transform: static (ctx, _) => GetUserRequestInfo(ctx))
            .Where(m => m is not null);

        // Collect user requests from referenced assemblies
        var referencedRequestsProvider = context.CompilationProvider
            .SelectMany((compilation, _) => GetAllUserRequestBaseTypes(compilation)
                .Select(GetUserRequestInfoFromSymbol));

        // Combine both sources
        var allRequestsProvider = requestsProvider
            .Collect()
            .Combine(referencedRequestsProvider.Collect())
            .SelectMany((pair, _) => pair.Left.Concat(pair.Right))
            .Where(m => m is not null);

        var currentProjectRequestsProvider = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsClassDeclarationSyntaxWithBaseList(s),
                transform: static (ctx, _) => GetUserRequestInfo(ctx))
            .Where(m => m is not null);

        // Generate source code
        var userRequests = currentProjectRequestsProvider.Collect(); // allRequestsProvider.Collect();
        context.RegisterSourceOutput(userRequests, static (ctx, regs) =>
        {
            foreach (var userRequest in regs)
            {
                if (userRequest is null)
                {
                    continue;
                }

                ctx.AddSource(
                    $"{userRequest.Value.Name}WithCommandKey.g.cs",
                    SourceText.From(GetUserRequestWithCommandKey(userRequest.Value), Encoding.UTF8));
            }
            /*
            ctx.AddSource(
                "UserRequestFactory.g.cs",
                SourceText.From(GetUserRequestFactoryPartialClass(regs), Encoding.UTF8));
            ctx.AddSource(
                "HelpResponseFactory.g.cs",
                SourceText.From(GetHelpRequestFactoryPartialClass(regs), Encoding.UTF8));
            */
        });
    }
    
    private static UserRequestInfo? GetUserRequestInfoFromSymbol(INamedTypeSymbol type)
    {
        var userCommandAttribute = GetUserCommandAttributeValue(type);

        return new UserRequestInfo
        {
            Name = type.Name,
            CommandKey = userCommandAttribute.UserCommand,
            HelpText = string.IsNullOrEmpty(userCommandAttribute.HelpText) ? "" : GetHelpText(type, userCommandAttribute.HelpText!),
            Namespace = type.ContainingNamespace.ToDisplayString()
        };
    }

    private static IEnumerable<INamedTypeSymbol> GetAllUserRequestBaseTypes(Compilation compilation)
    {
        var userRequestBaseType = compilation.GetTypeByMetadataName("Moneo.Chat.UserRequestBase");
        if (userRequestBaseType == null)
        {
            yield break;
        }

        foreach (var assembly in compilation.ReferencedAssemblyNames)
        {
            var referencedAssembly = compilation.References
                .Select(compilation.GetAssemblyOrModuleSymbol)
                .OfType<IAssemblySymbol>()
                .FirstOrDefault(a => a.Identity.Name == assembly.Name);

            if (referencedAssembly == null)
            {
                continue;
            }

            foreach (var type in GetAllTypesInNamespace(referencedAssembly.GlobalNamespace))
            {
                if (type.BaseType?.Equals(userRequestBaseType, SymbolEqualityComparer.Default) == true)
                {
                    yield return type;
                }
            }
        }
    }

    private static IEnumerable<INamedTypeSymbol> GetAllTypesInNamespace(INamespaceSymbol namespaceSymbol)
    {
        foreach (var member in namespaceSymbol.GetMembers())
        {
            if (member is INamedTypeSymbol typeSymbol)
            {
                yield return typeSymbol;
            }
            else if (member is INamespaceSymbol childNamespace)
            {
                foreach (var nestedType in GetAllTypesInNamespace(childNamespace))
                {
                    yield return nestedType;
                }
            }
        }
    }

    private static string GetHelpRequestFactoryPartialClass(ImmutableArray<UserRequestInfo?> regs)
    {
        var userRequests = regs.Where(r => r.HasValue && !string.IsNullOrEmpty(r.Value.HelpText))
            .Select(r => r!.Value)
            .OrderBy(r => r.CommandKey)
            .ToImmutableArray();

        if (userRequests.Length == 0)
        {
            return string.Empty;
        }

        var defaultHelpText = new StringBuilder();
        defaultHelpText.AppendLine("Available Commands");
        defaultHelpText.AppendLine("------------------");
        defaultHelpText.AppendLine();

        var usingStatementsBuilder = new StringBuilder();
        var initializedLookupBuilder = new StringBuilder();

        var visitedNamespaces = new HashSet<string>();

        foreach (var request in userRequests)
        {
            if (visitedNamespaces.Add(request.Namespace))
            {
                usingStatementsBuilder.AppendLine($"using {request.Namespace};");
            }
            initializedLookupBuilder.AppendLine($"        _lookup[{request.Name}.CommandKey[1..]] = @\"{request.HelpText}\";");
            defaultHelpText.AppendLine(request.CommandKey);
        }

        var codeBuilder = new StringBuilder();
        codeBuilder.Append(usingStatementsBuilder);
        codeBuilder.AppendLine();
        codeBuilder.AppendLine("namespace Moneo.Chat;");
        codeBuilder.AppendLine();
        codeBuilder.AppendLine("public static partial class HelpResponseFactory");
        codeBuilder.AppendLine("{");
        codeBuilder.AppendLine();
        codeBuilder.AppendLine($"  public static readonly string DefaultHelpResponse = @\"{defaultHelpText}\";");
        codeBuilder.AppendLine("  private static void InitializeLookup()");
        codeBuilder.AppendLine("  {");
        codeBuilder.Append(initializedLookupBuilder);
        codeBuilder.AppendLine("  }");
        codeBuilder.AppendLine("}");

        return codeBuilder.ToString();
    }

    private static string GetUserRequestWithCommandKey(UserRequestInfo userRequest)
    {
        if (userRequest == null) throw new ArgumentNullException(nameof(userRequest));
        var builder = new StringBuilder("// Generated By Source Generator");
        builder.AppendLine();
        builder.AppendLine($"namespace {userRequest.Namespace};");
        builder.AppendLine();
        builder.AppendLine($"public partial class {userRequest.Name}");
        builder.AppendLine("{");
        builder.AppendLine($"    public const string {AttributeKeys.CommandKey} = \"{userRequest.CommandKey}\";");
        builder.AppendLine();
        builder.AppendLine("    public static void Register()");
        builder.AppendLine("    {");
        builder.AppendLine($"        UserRequestFactory.RegisterCommand({AttributeKeys.CommandKey}, (id, args) => new {userRequest.Name}(id, args));");
        
        if (!string.IsNullOrEmpty(userRequest.HelpText))
        {
            builder.AppendLine(
                $"        HelpResponseFactory.RegisterCommand({AttributeKeys.CommandKey}, @\"{userRequest.HelpText}\");");
        }
        
        builder.AppendLine("    }");
        builder.AppendLine("}");
        builder.AppendLine();
        return builder.ToString();
    }

    private static bool IsClassDeclarationSyntaxWithBaseList(SyntaxNode node)
    {
        if (node is not ClassDeclarationSyntax {BaseList: not null} classDeclaration)
        {
            return false;
        }

        return classDeclaration.BaseList.Types.Any(t => t.ToString().Equals("UserRequestBase"));
    }

    private static UserRequestInfo? GetUserRequestInfo(GeneratorSyntaxContext context)
    {
        var semanticModel = context.SemanticModel;
        if (semanticModel.GetDeclaredSymbol(context.Node) is not INamedTypeSymbol type)
        {
            return null;
        }

        var userCommandAttribute = GetUserCommandAttributeValue(type);

        return new UserRequestInfo
        {
            Name = type.Name,
            CommandKey = userCommandAttribute.UserCommand,
            HelpText = string.IsNullOrEmpty(userCommandAttribute.HelpText) ? "" : GetHelpText(type, userCommandAttribute.HelpText!),
            Namespace = type.ContainingNamespace.ToDisplayString()
        };
    }

    private static string GetHelpText(INamedTypeSymbol userRequest, string requestHelpText)
    {
        var helpTextBuilder = new StringBuilder();
        helpTextBuilder.AppendLine(requestHelpText);

        foreach (var member in userRequest.GetMembers().Where(x => x.Kind == SymbolKind.Property))
        {
            var propertySymbol = (IPropertySymbol)member;
            var argAttributeData = GetUserCommandArgumentAttributeValue(propertySymbol);

            if (argAttributeData is null)
            {
                continue;
            }

            var (longName, shortName, helpText, isRequired, isHidden) = argAttributeData.Value;

            if (isHidden)
            {
                continue;
            }

            helpTextBuilder.AppendLine();
            helpTextBuilder.Append(longName ?? propertySymbol.Name);
            if (isRequired)
            {
                helpTextBuilder.Append(" (required)");
            }

            helpTextBuilder.AppendLine();

            if (!string.IsNullOrWhiteSpace(helpText))
            {
                helpTextBuilder.AppendLine("\t" + helpText);
            }
        }

        return helpTextBuilder.ToString();
    }

    private static (string? LongName, string? ShortName, string? HelpText, bool IsRequired, bool IsHidden)?
        GetUserCommandArgumentAttributeValue(ISymbol symbol)
    {
        var attribute = symbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == AttributeKeys.UserCommandArgumentAttribute);

        if (attribute is null)
        {
            return null;
        }

        var longNameArg = attribute.NamedArguments.FirstOrDefault(arg => arg.Key == "LongName");
        var shortNameArg = attribute.NamedArguments.FirstOrDefault(arg => arg.Key == "ShortName");
        var helpTextArg = attribute.NamedArguments.FirstOrDefault(arg => arg.Key == "HelpText");
        var isRequiredArg = attribute.NamedArguments.FirstOrDefault(arg => arg.Key == "IsRequired");
        var isHiddenArg = attribute.NamedArguments.FirstOrDefault(arg => arg.Key == "IsHidden");

        if (longNameArg.Key != null)
        {
            return (longNameArg.Value.Value?.ToString(), shortNameArg.Value.Value?.ToString(),
                helpTextArg.Value.Value?.ToString(), (bool) (isRequiredArg.Value.Value ?? false),
                (bool) (isHiddenArg.Value.Value ?? false));
        }

        return null;
    }

    private static UserCommandAttributeData GetUserCommandAttributeValue(ISymbol symbol)
    {
        var attribute = symbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == AttributeKeys.UserCommandAttribute);

        if (attribute is not null)
        {
            var commandKeyArg = attribute.NamedArguments.FirstOrDefault(arg => arg.Key.Equals(AttributeKeys.CommandKey, StringComparison.OrdinalIgnoreCase));
            var helpTextArg = attribute.NamedArguments.FirstOrDefault(arg => arg.Key.Equals(AttributeKeys.UserCommandHelpText, StringComparison.OrdinalIgnoreCase));

            if (commandKeyArg.Key != null)
            {
                return new UserCommandAttributeData
                {
                    UserCommand = commandKeyArg.Value.Value?.ToString(),
                    HelpText = helpTextArg.Value.Value?.ToString()
                };
            }
        }

        throw new InvalidOperationException($"{symbol.Name} must be marked with a UserCommandAttribute");
    }

    private static string GetUserRequestFactoryPartialClass(ImmutableArray<UserRequestInfo?> userRequests)
    {
        var requests = userRequests.Where(r => r.HasValue).Select(r => r.Value).ToImmutableArray();
        var usings = requests.Select(r => $"using {r.Namespace};").Distinct();

        var sourceBuilder = new StringBuilder();
        foreach (var @using in usings)
        {
            sourceBuilder.AppendLine(@using);
        }
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("namespace Moneo.Chat;");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("public static partial class UserRequestFactory");
        sourceBuilder.AppendLine("{");
        sourceBuilder.AppendLine("  static UserRequestFactory()");
        sourceBuilder.AppendLine("  {");

        // foreach loop here
        foreach (var userRequest in requests)
        {
            sourceBuilder.AppendLine($"    RegisterCommand({userRequest.Name}.CommandKey, (id, args) => new {userRequest.Name}(id, args));");
            //sourceBuilder.AppendLine($"    _lookup[{userRequest.Name}.CommandKey] = (id, args) => new {userRequest.Name}(id, args);");
        }

        sourceBuilder.AppendLine("  }");
        sourceBuilder.AppendLine("}");

        return sourceBuilder.ToString();
    }
}