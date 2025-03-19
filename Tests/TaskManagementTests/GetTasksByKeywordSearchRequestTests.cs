using Moneo.TaskManagement.Api.Features.GetTasks;
using Moneo.TaskManagement.ResourceAccess.Entities;

namespace TaskManagementTests;

public class GetTasksByKeywordSearchRequestTests
{
    private readonly TestFixture _fixture;
    private readonly GetTasksByKeywordSearchRequestHandler _handler;

    public GetTasksByKeywordSearchRequestTests()
    {
        _fixture = new TestFixture();
        _fixture.SetUtcNow(new DateTime(2024, 7, 3, 12, 32, 27));
        _handler = new GetTasksByKeywordSearchRequestHandler(_fixture.DbContext);
    }

    [Fact]
    public async Task Handle_NoTasksFound_ReturnsTaskNotFound()
    {
        // Arrange
        _fixture.ResetDbContext();

        var existingConversationIds = _fixture.GetConversationIds();
        var missingId = existingConversationIds.Max() + 1000;
        
        var request = new GetTasksByKeywordSearchRequest(missingId, "test");

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal($"No tasks found for conversation {missingId}", result.Message);
    }

    [Fact]
    public async Task Handle_TasksFoundWithMatchingKeywords_ReturnsMatchingTasks()
    {
        // Arrange
        _fixture.ResetDbContext();
        var existingData = _fixture.InitConversationWithTasks(
        [

            new TaskEntry("Test Task", "Pacific"),
            new TaskEntry("Another Task", "Pacific")
        ]);
        var request = new GetTasksByKeywordSearchRequest(existingData.Conversation.Id, "Test");

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result!.Data!.Data!);
        
        var task = existingData.Tasks.Single(t => t.Name == "Test Task");
        
        Assert.Equal(task.Id, result.Data!.Data![0].Id);
    }

    [Fact]
    public async Task Handle_TasksFoundWithoutMatchingKeywords_ReturnsTaskNotFound()
    {
        // Arrange
        _fixture.ResetDbContext();
        var existingData = _fixture.InitConversationWithTasks(
        [

            new TaskEntry("Test Task", "Pacific"),
            new TaskEntry("Another Task", "Pacific")
        ]);
        var request = new GetTasksByKeywordSearchRequest(existingData.Conversation.Id, "NonMatchingKeyword");

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("No tasks found matching the search criteria", result.Message);
    }
    
    [Fact]
    public async Task Handle_WhenMultipleTasksFound_ReturnsMatchingTasks()
    {
        // Arrange
        _fixture.ResetDbContext();
        var existingData = _fixture.InitConversationWithTasks(
        [

            new TaskEntry("Test Task", "Pacific"),
            new TaskEntry("Another Task", "Pacific")
        ]);
        var request = new GetTasksByKeywordSearchRequest(existingData.Conversation.Id, "Task");

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result!.Data!.Data!.Count);
    }
}
