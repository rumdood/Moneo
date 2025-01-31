using MediatR;
using Moneo.Common;
using Moneo.TaskManagement.Api.Features.CompleteTask;
using Moneo.TaskManagement.Contracts;
using Moneo.TaskManagement.Contracts.Models;
using Moneo.TaskManagement.Features.CreateEditTask;
using Moneo.TaskManagement.Features.DeactivateTask;
using Moneo.TaskManagement.Features.DeleteTask;
using Moneo.TaskManagement.Features.GetTaskById;
using Moneo.TaskManagement.Features.GetTasks;

namespace Moneo.TaskManagement.Api.Services;

internal class InternalTaskManagerClient : ITaskManagerClient
{
    private readonly IMediator _mediator;
    
    public InternalTaskManagerClient(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<MoneoResult<PagedList<MoneoTaskDto>>> GetTasksForConversationAsync(
        long conversationId,
        PageOptions pagingOptions, 
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetTasksForConversationRequest(conversationId, pagingOptions),
            cancellationToken);

        return result;
    }

    public Task<MoneoResult<PagedList<MoneoTaskDto>>> GetTasksForUserAsync(long userId, PageOptions pagingOptions,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<MoneoResult<PagedList<MoneoTaskDto>>> GetTasksForUserAndConversationAsync(long userId, long conversationId,
        PageOptions pagingOptions, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<MoneoResult<PagedList<MoneoTaskDto>>> GetTasksByKeywordSearchAsync(long conversationId, string keyword, PageOptions pagingOptions,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<MoneoResult<MoneoTaskDto>> GetTaskAsync(long taskId, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetTaskByIdRequest(taskId), cancellationToken);
        if (result.IsSuccess)
        {
            return MoneoResult<MoneoTaskDto>.Success(result.Data);
        }

        return new MoneoResult<MoneoTaskDto>
        {
            Exception = result.Exception, 
            Message = result.Message, 
            Type = result.Type
        };
    }

    public async Task<MoneoResult<MoneoTaskDto>> CreateTaskAsync(long conversationId, CreateEditTaskDto dto, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new CreateEditTaskRequest(dto, ConversationId: conversationId), cancellationToken);

        if (!result.IsSuccess)
        {
            return new MoneoResult<MoneoTaskDto>
            {
                Exception = result.Exception, 
                Message = result.Message, 
                Type = result.Type
            };
        }
        
        var getResult = await GetTaskAsync(result.Data, cancellationToken);

        return getResult;
    }

    public async Task<MoneoResult> UpdateTaskAsync(
        long taskId, 
        CreateEditTaskDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new CreateEditTaskRequest(dto, TaskId: taskId), cancellationToken);
        return result.IsSuccess
            ? MoneoResult.Success()
            : new MoneoResult
            {
                Exception = result.Exception,
                Message = result.Message,
                Type = result.Type
            };
    }

    public async Task<MoneoResult> DeleteTaskAsync(long taskId, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new DeleteTaskRequest(taskId), cancellationToken);
        return result;
    }

    public async Task<MoneoResult> CompleteTaskAsync(long taskId, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new CompleteOrSkipTaskRequest(taskId, TaskCompletionType.Completed), cancellationToken);
        return result;
    }

    public async Task<MoneoResult> SkipTaskAsync(long taskId, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new CompleteOrSkipTaskRequest(taskId, TaskCompletionType.Skipped), cancellationToken);
        return result;
    }

    public async Task<MoneoResult> DeactivateTaskAsync(long taskId, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new DeactivateTaskRequest(taskId), cancellationToken);
        return result;
    }
}