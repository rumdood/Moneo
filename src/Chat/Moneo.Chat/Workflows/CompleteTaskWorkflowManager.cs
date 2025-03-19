using MediatR;
using Moneo.Chat.Commands;
using Moneo.Common;
using Moneo.TaskManagement.Contracts;

namespace Moneo.Chat.Workflows;

public enum CompleteTaskOption
{
    Complete,
    Skip
}

public interface ICompleteTaskWorkflowManager : IWorkflowManager
{
    Task<MoneoCommandResult> StartWorkflowAsync(long conversationId, string taskName, CompleteTaskOption option,
        CancellationToken cancellationToken = default);
}

public class CompleteTaskWorkflowManager : WorkflowManagerBase, ICompleteTaskWorkflowManager
{
    private readonly ITaskManagerClient _taskManagerClient;
    
    public async Task<MoneoCommandResult> StartWorkflowAsync(long conversationId, string taskName, CompleteTaskOption option, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(taskName))
        {
            return await Mediator.Send(new ListTasksRequest(conversationId, true), cancellationToken);
        }

        var tasksMatchingName = await _taskManagerClient.GetTasksByKeywordSearchAsync(conversationId, taskName,
            new PageOptions(0, 100), cancellationToken);

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
                UserMessageText = $"There are no active tasks called {taskName}"
            };
        }

        if (tasks.Length == 1)
        {
            var taskToComplete = tasks.First();
            // here we'll do a call to the Azure Function to complete the task
            var completeTaskResult = option switch
            {
                CompleteTaskOption.Complete => await _taskManagerClient.CompleteTaskAsync(taskToComplete.Id, cancellationToken),
                CompleteTaskOption.Skip => await _taskManagerClient.SkipTaskAsync(taskToComplete.Id, cancellationToken),
                _ => MoneoResult.Failed("No such option")
            };

            return new MoneoCommandResult
            {
                ResponseType = completeTaskResult.IsSuccess ? ResponseType.None : ResponseType.Text,
                Type = completeTaskResult.IsSuccess ? ResultType.WorkflowCompleted : ResultType.Error,
                UserMessageText = completeTaskResult.IsSuccess ? "" : "Something went wrong. Look at the logs?"
            };
        }

        var menuCommand = option switch
        {
            CompleteTaskOption.Complete => "/complete",
            CompleteTaskOption.Skip => "/skip",
            _ => throw new IndexOutOfRangeException($"No such option {option}")
        };
        
        return new MoneoCommandResult
        {
            ResponseType = ResponseType.Menu,
            Type = ResultType.NeedMoreInfo,
            UserMessageText = "There were multiple possible tasks that matched the description you gave",
            MenuOptions = tasks.Select(t => $"{menuCommand} {t.Name}").ToHashSet()
        };
    }

    public CompleteTaskWorkflowManager(IMediator mediator, ITaskManagerClient taskManagerClient) : base(mediator)
    {
        _taskManagerClient = taskManagerClient;
    }
}