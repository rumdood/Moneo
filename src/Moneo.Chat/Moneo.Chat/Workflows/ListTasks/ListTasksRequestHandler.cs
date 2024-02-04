using MediatR;
using Moneo.Chat.Commands;
using Moneo.TaskManagement;
using Moneo.TaskManagement.Models;

namespace Moneo.Chat.UserRequests;

internal class ListTasksRequestHandler : IRequestHandler<ListTasksRequest, MoneoCommandResult>
{
    private readonly ITaskResourceManager _taskResourceManager;
    
    public ListTasksRequestHandler(ITaskResourceManager taskResourceManager)
    {
        _taskResourceManager = taskResourceManager;
    }
    
    public async Task<MoneoCommandResult> Handle(ListTasksRequest request, CancellationToken cancellationToken)
    {
        var ( _, taskList, _) = await _taskResourceManager.GetAllTasksForUserAsync(request.ConversationId);

        return new MoneoCommandResult
        {
            ResponseType = request.AsMenuFlag ? ResponseType.Menu : ResponseType.Text,
            Type = ResultType.WorkflowCompleted,
            UserMessageText = request.AsMenuFlag ? "Please select a task:" : GetTaskListAsString(taskList),
            MenuOptions = [..request.AsMenuFlag ? GetTaskListAsMenuOptions(taskList) : Array.Empty<string>()]
        };
    }

    private static string GetTaskListAsString(IEnumerable<MoneoTaskDto>? tasks)
    {
        var taskArray = tasks?.Where(x => x.IsActive).ToArray();
        if (taskArray is null || taskArray.Length == 0)
        {
            return "No tasks found";
        }

        return string.Join('\n', taskArray.Where(x => x.IsActive).Select(x => x.Name));
    }

    private static string[] GetTaskListAsMenuOptions(IEnumerable<MoneoTaskDto>? tasks)
    {
        var taskArray = tasks?.Where(x => x.IsActive).ToArray();
        if (taskArray is null || taskArray.Length == 0)
        {
            return ["No tasks found"];
        }

        return taskArray.Select(x => x.Name).ToArray();
    }
}