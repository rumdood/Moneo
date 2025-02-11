using Moneo.Common;
using Moneo.TaskManagement.Contracts;
using Moneo.TaskManagement.Contracts.Models;

namespace Moneo.Hosts.Chat.Api.Tasks;

internal class TasksClient : ITaskManagerClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string _apiKey;

    public TasksClient(HttpClient httpClient, TaskManagementConfig config)
    {
        _baseUrl = config.BaseUrl;
        _apiKey = config.ApiKey;
        _httpClient = httpClient;
    }

    private async Task<MoneoResult<T>> SendRequestAsync<T>(HttpMethod method, string uri, object? content = null,
        CancellationToken cancellationToken = default)
    {
        var requestUri = new Uri(new Uri(_baseUrl), uri);
        var requestMessage = new HttpRequestMessage(method, requestUri)
        {
            Content = JsonContent.Create(content)
        };

        if (!string.IsNullOrEmpty(_apiKey))
        {
            requestMessage.Headers.Add("ApiKey", _apiKey);
        }

        var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<MoneoResult<T>>(cancellationToken: cancellationToken);
            return result ?? MoneoResult<T>.Failed("Failed to deserialize response.");
        }

        var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
        return MoneoResult<T>.Failed($"Error: {errorContent}");
    }
    
    public async Task<MoneoResult<MoneoTaskDto>> CreateTaskAsync(long conversationId, CreateEditTaskDto dto, CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<MoneoTaskDto>(HttpMethod.Post, $"api/conversations/{conversationId}/tasks", dto, cancellationToken);
    }

    public async Task<MoneoResult> UpdateTaskAsync(long taskId, CreateEditTaskDto dto, CancellationToken cancellationToken = default)
    {
        var result = await SendRequestAsync<object>(HttpMethod.Put, $"api/tasks/{taskId}", dto, cancellationToken);
        return result.IsSuccess ? MoneoResult.Success() : MoneoResult.Failed(result.Message);
    }
    
    public async Task<MoneoResult> CompleteTaskAsync(long taskId, CancellationToken cancellationToken = default)
    {
        var result = await SendRequestAsync<object>(HttpMethod.Post, $"api/tasks/{taskId}/complete", null, cancellationToken);
        return result.IsSuccess ? MoneoResult.Success() : MoneoResult.Failed(result.Message);
    }

    public async Task<MoneoResult> SkipTaskAsync(long taskId, CancellationToken cancellationToken = default)
    {
        var result = await SendRequestAsync<object>(HttpMethod.Post, $"api/tasks/{taskId}/skip", null, cancellationToken);
        return result.IsSuccess ? MoneoResult.Success() : MoneoResult.Failed(result.Message);
    }

    public async Task<MoneoResult<PagedList<MoneoTaskDto>>> GetTasksForConversationAsync(long conversationId,
        PageOptions pagingOptions, CancellationToken cancellationToken = default)
    {
        var query =
            $"api/conversations/{conversationId}/tasks?pn={pagingOptions.PageNumber}&ps={pagingOptions.PageSize}";
        return await SendRequestAsync<PagedList<MoneoTaskDto>>(HttpMethod.Get, query, null, cancellationToken);
    }

    public async Task<MoneoResult<PagedList<MoneoTaskDto>>> GetTasksForUserAsync(long userId, PageOptions pagingOptions,
        CancellationToken cancellationToken = default)
    {
        var query = $"api/users/{userId}/tasks?pn={pagingOptions.PageNumber}&ps={pagingOptions.PageSize}";
        return await SendRequestAsync<PagedList<MoneoTaskDto>>(HttpMethod.Get, query, null, cancellationToken);
    }

    public async Task<MoneoResult<PagedList<MoneoTaskDto>>> GetTasksForUserAndConversationAsync(long userId,
        long conversationId, PageOptions pagingOptions, CancellationToken cancellationToken = default)
    {
        var query =
            $"api/users/{userId}/conversations/{conversationId}/tasks?pn={pagingOptions.PageNumber}&ps={pagingOptions.PageSize}";
        return await SendRequestAsync<PagedList<MoneoTaskDto>>(HttpMethod.Get, query, null, cancellationToken);
    }

    public async Task<MoneoResult<PagedList<MoneoTaskDto>>> GetTasksByKeywordSearchAsync(long conversationId,
        string keyword, PageOptions pagingOptions, CancellationToken cancellationToken = default)
    {
        var query =
            $"api/conversations/{conversationId}/tasks/search?keyword={keyword}&pn={pagingOptions.PageNumber}&ps={pagingOptions.PageSize}";
        return await SendRequestAsync<PagedList<MoneoTaskDto>>(HttpMethod.Get, query, null, cancellationToken);
    }

    public async Task<MoneoResult<MoneoTaskDto>> GetTaskAsync(long taskId,
        CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<MoneoTaskDto>(HttpMethod.Get, $"api/tasks/{taskId}", null, cancellationToken);
    }

    public async Task<MoneoResult> DeleteTaskAsync(long taskId, CancellationToken cancellationToken = default)
    {
        var result = await SendRequestAsync<object>(HttpMethod.Delete, $"api/tasks/{taskId}", null, cancellationToken);
        return result.IsSuccess ? MoneoResult.Success() : MoneoResult.Failed(result.Message);
    }

    public async Task<MoneoResult> DeactivateTaskAsync(long taskId, CancellationToken cancellationToken = default)
    {
        var result =
            await SendRequestAsync<object>(HttpMethod.Post, $"api/tasks/{taskId}/deactivate", null, cancellationToken);
        return result.IsSuccess ? MoneoResult.Success() : MoneoResult.Failed(result.Message);
    }
}