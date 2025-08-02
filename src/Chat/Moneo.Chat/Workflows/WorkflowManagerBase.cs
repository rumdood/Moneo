using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Moneo.Chat.Workflows;

public abstract class WorkflowManagerBase
{
    protected readonly IMediator Mediator;

    protected WorkflowManagerBase(IMediator mediator)
    {
        Mediator = mediator;
    }
}

public abstract class WorkflowManagerWithDbContextBase<TContext> : WorkflowManagerBase where TContext : class
{
    protected readonly IServiceScopeFactory ScopeFactory;
    
    protected WorkflowManagerWithDbContextBase(IMediator mediator, IServiceScopeFactory scopeFactory) : base(mediator)
    {
        ScopeFactory = scopeFactory;
    }

    protected AsyncServiceScope GetScope() => ScopeFactory.CreateAsyncScope();
}

public static class AsyncServiceScopeExtensions
{
    public static TContext GetDbContext<TContext>(this AsyncServiceScope scope) where TContext : class
    {
        return scope.ServiceProvider.GetRequiredService<TContext>();
    }
}
