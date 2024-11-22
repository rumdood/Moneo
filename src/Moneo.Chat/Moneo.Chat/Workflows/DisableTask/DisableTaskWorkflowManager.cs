using MediatR;
using Moneo.Chat.Commands;
using Moneo.TaskManagement;
using Moneo.TaskManagement.Client.Models;

namespace Moneo.Chat.Workflows.DisableTask;

public interface IDisableTaskWorkflowManager : IWorkflowManager
{
    Task<MoneoCommandResult> StartWorkflowAsync(long conversationId, string taskName, CancellationToken cancellationToken = default);
}

public class DisableTaskWorkflowManager : WorkflowManagerBase, IDisableTaskWorkflowManager
{
    private readonly ITaskResourceManager _taskResourceManager;

    public async Task<MoneoCommandResult> StartWorkflowAsync(long conversationId, string taskName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(taskName))
        {
            return await Mediator.Send(new ListTasksRequest(conversationId, true), cancellationToken);
        }

        var tasksMatchingName =
            await _taskResourceManager.GetTasksForUserAsync(conversationId,
                new MoneoTaskFilter { SearchString = taskName });

        if (!tasksMatchingName.IsSuccessful || !tasksMatchingName.Result.Any())
        {
            return new MoneoCommandResult
            {
                ResponseType = ResponseType.Text,
                Type = ResultType.Error,
                UserMessageText = $"You don't have a task called {taskName} or anything like it"
            };
        }

        var tasks = tasksMatchingName.Result.Where(t => t.IsActive && !string.IsNullOrEmpty(t.Id)).ToArray();

        if (!tasks.Any())
        {
            return new MoneoCommandResult
            {
                ResponseType = ResponseType.Text,
                Type = ResultType.Error,
                UserMessageText = $"There was a problem working with {taskName} and I can't disable it."
            };
        }

        if (tasks.Length == 1)
        {
            // here we'll do a call to the Azure Function to disable the task
            var disableTaskResult = await _taskResourceManager.DisableTaskAsync(conversationId, tasks.First().Id);

            if (!disableTaskResult.IsSuccessful)
            {
                return new MoneoCommandResult
                {
                    ResponseType = ResponseType.Text,
                    Type = ResultType.Error,
                    UserMessageText = $"There was a problem working with {taskName} and I can't disable it."
                };
            }

            return new MoneoCommandResult
            {
                ResponseType = ResponseType.Text,
                Type = ResultType.WorkflowCompleted,
                UserMessageText = $"I've disabled the task {taskName}"
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

    public DisableTaskWorkflowManager(IMediator mediator, ITaskResourceManager taskResourceManager) : base(mediator)
    {
        _taskResourceManager = taskResourceManager;
    }
}
