using Moneo.TaskManagement.Contracts.Models;
using Moneo.TaskManagement.Features.GetTaskById;
using Moneo.TaskManagement.ResourceAccess;

namespace Moneo.Tests.TaskManagementApiTests;

public class GetTaskByIdRequestHandlerTests : TaskManagementApiTestBase
{
    private static GetTaskByIdRequestHandler GetHandler(MoneoTasksDbContext dbContext)
        => new(dbContext);

    [Fact]
    public async Task Handle_ReturnsTask_WhenTaskExists()
    {
        // Arrange
        var conversation = Fixture.CreateConversations().Single();
        var task = Fixture.CreateTasks(conversationId: conversation.Id).Single();
        var request = new GetTaskByIdRequest(task.Id);
        var handler = GetHandler(DbContext);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(task.Name, result.Data.Name);
    }

    [Fact]
    public async Task Handle_ReturnsFailedResult_WhenTaskDoesNotExist()
    {
        // Arrange
        var request = new GetTaskByIdRequest(0); // Invalid task ID to trigger failure
        var handler = GetHandler(DbContext);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Task not found", result.Message);
    }

    [Fact]
    public async Task Handle_ReturnsFailedResult_WhenExceptionThrown()
    {
        // Arrange
        var request = new GetTaskByIdRequest(-1); // Invalid task ID to trigger exception
        var handler = GetHandler(DbContext);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Message);
        Assert.Equal("Task not found", result.Message);
    }

    [Fact]
    public async Task Handle_ReturnsTaskWithCompletionData_WhenTaskHasCompletionData()
    {
        // Arrange
        var conversation = Fixture.CreateConversations().Single();
        var task = Fixture.CreateTasks(conversationId: conversation.Id).Single();
        // Simulate task completion
        var completedEvent = Fixture.CreateTaskEventForTask(task.Id,
            TimeProvider.Object.GetUtcNow().AddHours(-1).UtcDateTime, TaskEventType.Completed);

        var request = new GetTaskByIdRequest(task.Id);
        var handler = GetHandler(DbContext);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(task.Name, result.Data.Name);
        Assert.NotNull(result.Data.LastCompleted);
    }
}