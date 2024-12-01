using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Moneo.Chat.Generators;

internal class WorkflowManagerRegistrationInfo
{
    public string InterfaceName { get; set; }
    public string ImplementationName { get; set; }
}


[Generator]
public class ServiceCollectionExtensionsGenerator : IIncrementalGenerator
{
    private const string ServiceCollectionExtensionsNamespace = "Moneo.Chat.ServiceCollectionExtensions";
    private const string WorkflowManagerNamespace = "Moneo.Chat.Workflows";
    private const string MarkerInterfaceName = "IWorkflowManager";
    
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<WorkflowManagerRegistrationInfo?> registrations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsWorkflowManagerClass(s),
                transform: static (ctx, _) => GetWorkflowManagerClassDeclaration(ctx))
            .Where(m => m is not null);

        var collected = registrations.Collect();
        context.RegisterSourceOutput(collected, static (ctx, regs) =>
        {
            ctx.AddSource("ServiceCollectionExtensions.g.cs", GenerateServiceCollectionExtensions(regs));
        });
    }

    private static bool IsWorkflowManagerClass(SyntaxNode node)
    {
        return node is ClassDeclarationSyntax {BaseList: not null};
    }
    
    private static WorkflowManagerRegistrationInfo? GetWorkflowManagerClassDeclaration(GeneratorSyntaxContext context)
    {
        if (context.Node is not ClassDeclarationSyntax classDeclaration)
        {
            return null;
        }

        var markerInterface = context.SemanticModel.Compilation.GetTypeByMetadataName($"{WorkflowManagerNamespace}.{MarkerInterfaceName}");

        if (markerInterface is null)
        {
            return null;
        }

        var semanticModel = context.SemanticModel;
        if (semanticModel.GetDeclaredSymbol(classDeclaration) is not INamedTypeSymbol type)
        {
            return null;
        }

        var interfaces = type.AllInterfaces
            .Where(i => i.AllInterfaces.Contains(markerInterface));

        var workflowInterface = interfaces.FirstOrDefault();
        
        if (workflowInterface is null)
        {
            return null;
        }

        return new WorkflowManagerRegistrationInfo
        {
            InterfaceName = workflowInterface.ToDisplayString(),
            ImplementationName = type.ToDisplayString()
        };
    }
    
    private static string GenerateServiceCollectionExtensions(ImmutableArray<WorkflowManagerRegistrationInfo?> workflowManagerRegistrations)
    {
        if (workflowManagerRegistrations.Length == 0)
        {
            return string.Empty;
        }
        
        var nonNullRegistrations = workflowManagerRegistrations.Where(r => r is not null).ToArray();
        if (nonNullRegistrations.Length == 0)
        {
            return string.Empty;
        }

        var sourceBuilder = new StringBuilder();
        sourceBuilder.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        sourceBuilder.AppendLine($"using {WorkflowManagerNamespace};");
        sourceBuilder.AppendLine();

        sourceBuilder.AppendLine($"namespace {ServiceCollectionExtensionsNamespace}");
        sourceBuilder.AppendLine("{");
        sourceBuilder.AppendLine("    public static partial class ServiceCollectionExtensions");
        sourceBuilder.AppendLine("    {");
        sourceBuilder.AppendLine("        public static IServiceCollection AddWorkflowManagers(this IServiceCollection services) =>");
        sourceBuilder.AppendLine("            services");

        foreach (var registration in nonNullRegistrations)
        {
            sourceBuilder.AppendLine($"                .AddSingleton<{registration!.InterfaceName}, {registration!.ImplementationName}>()");
        }

        sourceBuilder.AppendLine("            ;");
        sourceBuilder.AppendLine("    }");
        sourceBuilder.AppendLine("}");

        return sourceBuilder.ToString();
    }

    private static IReadOnlyList<INamedTypeSymbol> GetWorkflowManagerInterfaces(ClassDeclarationSyntax classDeclaration,
        SemanticModel semanticModel)
    {
        if (classDeclaration.BaseList is null)
        {
            return Array.Empty<INamedTypeSymbol>();
        }
        
        var implemented = new List<INamedTypeSymbol>();
        foreach (var baseType in classDeclaration.BaseList.Types)
        {
            var symbolInfo = semanticModel.GetSymbolInfo(baseType.Type);
            if (symbolInfo.Symbol is INamedTypeSymbol namedTypeSymbol)
            {
                implemented.Add(namedTypeSymbol);
            }
        }

        return implemented;
    }
}