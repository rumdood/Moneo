using Moneo.Chat.Commands;

namespace Moneo.Chat.Workflows.CreateCronSchedule;

public interface ICreateCronWorkflowManager : IWorkflowManagerWithContinuation
{
    Task<MoneoCommandResult> StartWorkflowAsync(CommandContext cmdContext, CancellationToken cancellationToken = default);
}