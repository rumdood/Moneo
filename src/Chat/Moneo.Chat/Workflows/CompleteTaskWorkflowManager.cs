using MediatR;
using Moneo.Chat.Commands;
using Moneo.Obsolete.TaskManagement;
using Moneo.Obsolete.TaskManagement.Client.Models;

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
    private readonly ITaskResourceManager _taskResourceManager;
    
    public async Task<MoneoCommandResult> StartWorkflowAsync(long conversationId, string taskName, CompleteTaskOption option, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(taskName))
        {
            return await Mediator.Send(new ListTasksRequest(conversationId, true), cancellationToken);
        }

        var tasksMatchingName =
            await _taskResourceManager.GetTasksForUserAsync(conversationId,
                new MoneoTaskFilter {SearchString = taskName});

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
                UserMessageText = $"There was a problem working with {taskName} and I can't clear it."
            };
        }

        if (tasks.Length == 1)
        {
            // here we'll do a call to the Azure Function to complete the task
            var completeTaskResult = option switch
            {
                CompleteTaskOption.Complete => await _taskResourceManager.CompleteTaskAsync(conversationId,
                    tasks.First().Id),
                CompleteTaskOption.Skip => await _taskResourceManager.SkipTaskAsync(conversationId, tasks.First().Id),
                _ => new MoneoTaskResult(false, "Unknown option")
            };

            return new MoneoCommandResult
            {
                ResponseType = completeTaskResult.IsSuccessful ? ResponseType.None : ResponseType.Text,
                Type = completeTaskResult.IsSuccessful ? ResultType.WorkflowCompleted : ResultType.Error,
                UserMessageText = completeTaskResult.IsSuccessful ? "" : "Something went wrong. Look at the logs?"
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

    public CompleteTaskWorkflowManager(IMediator mediator, ITaskResourceManager taskResourceManager) : base(mediator)
    {
        _taskResourceManager = taskResourceManager;
    }
}