using MediatR;
using Moneo.Chat;
using Moneo.Chat.Commands;
using Moneo.Chat.Workflows;
using Moneo.Common;
using Moneo.TaskManagement.Chat;
using Moneo.TaskManagement.Contracts;

namespace Moneo.TaskManagement.Workflows.DisableTask;

public interface IDisableTaskWorkflowManager : IWorkflowManager
{
    Task<MoneoCommandResult> StartWorkflowAsync(CommandContext cmdContext, string taskName, CancellationToken cancellationToken = default);
}

[MoneoWorkflow]
public class DisableTaskWorkflowManager : WorkflowManagerBase, IDisableTaskWorkflowManager
{
    private readonly ITaskManagerClient _taskManagerClient;

    public async Task<MoneoCommandResult> StartWorkflowAsync(CommandContext cmdContext, string taskName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(taskName))
        {
            return await Mediator.Send(new ListTasksRequest(cmdContext), cancellationToken);
        }

        var tasksMatchingName = await _taskManagerClient.GetTasksByKeywordSearchAsync(
            cmdContext.ConversationId, 
            taskName,
            new PageOptions(0, 100), 
            cancellationToken);

        if (!tasksMatchingName.IsSuccess || tasksMatchingName.Data?.TotalCount == 0)
        {
            return new MoneoCommandResult
            {
                ResponseType = ResponseType.Text,
                Type = ResultType.Error,
                UserMessageText = $"You don't have a task called {taskName} or anything like it"
            };
        }

        var tasks = tasksMatchingName.Data?.Data?.Where(t => t.IsActive).ToArray() ?? [];

        if (tasks.Length == 0)
        {
            return new MoneoCommandResult
            {
                ResponseType = ResponseType.Text,
                Type = ResultType.Error,
                UserMessageText = $"There were no active tasks similar to {taskName}"
            };
        }

        if (tasks.Length == 1)
        {
            var taskToDisable = tasks.First();
            // here we'll do a call to the Azure Function to disable the task
            var disableTaskResult = await _taskManagerClient.DeactivateTaskAsync(taskToDisable.Id, cancellationToken);

            if (!disableTaskResult.IsSuccess)
            {
                return new MoneoCommandResult
                {
                    ResponseType = ResponseType.Text,
                    Type = ResultType.Error,
                    UserMessageText = $"There was a problem working with {taskToDisable.Name} and I can't disable it."
                };
            }

            return new MoneoCommandResult
            {
                ResponseType = ResponseType.Text,
                Type = ResultType.WorkflowCompleted,
                UserMessageText = $"I've disabled the task \"{taskToDisable.Name}\""
            };
        }

        return new MoneoCommandResult
        {
            ResponseType = ResponseType.Menu,
            Type = ResultType.NeedMoreInfo,
            UserMessageText = "There were multiple possible tasks that matched the description you gave",
            MenuOptions = tasks.Select(t => $"/disable {t.Name}").ToHashSet()
        };
    }

    public DisableTaskWorkflowManager(IMediator mediator, ITaskManagerClient taskManagerClient) : base(mediator)
    {
        _taskManagerClient = taskManagerClient;
    }
}
