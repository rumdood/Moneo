using MediatR;
using Moneo.Chat.Commands;
using Moneo.Common;
using Moneo.TaskManagement.Contracts;
using Moneo.TaskManagement.Contracts.Models;

namespace Moneo.Chat.UserRequests;

internal class ListTasksRequestHandler : IRequestHandler<ListTasksRequest, MoneoCommandResult>
{
    private readonly ITaskManagerClient _taskManagerClient;
    
    public ListTasksRequestHandler(ITaskManagerClient taskManagerClient)
    {
        _taskManagerClient = taskManagerClient;
    }
    
    public async Task<MoneoCommandResult> Handle(ListTasksRequest request, CancellationToken cancellationToken)
    {
        var result = await _taskManagerClient.GetTasksForConversationAsync(
            request.Context.ConversationId,
            new PageOptions(0, 100), 
            cancellationToken);

        if (!result.IsSuccess)
        {
            return new MoneoCommandResult
            {
                ResponseType = ResponseType.Text,
                Type = ResultType.Error,
                UserMessageText = result.Message
            };
        }

        var taskList = result.Data?.Data ?? [];

        return new MoneoCommandResult
        {
            ResponseType = request.AsMenuFlag ? ResponseType.Menu : ResponseType.Text,
            Type = ResultType.WorkflowCompleted,
            UserMessageText = request.AsMenuFlag ? "Please select a task:" : GetTaskListAsString(taskList),
            MenuOptions = [..request.AsMenuFlag ? GetTaskListAsMenuOptions(taskList) : []]
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