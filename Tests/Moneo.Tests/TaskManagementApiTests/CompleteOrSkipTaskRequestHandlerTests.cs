using Moneo.TaskManagement.Api.Features.CompleteTask;
using Moneo.TaskManagement.ResourceAccess;

namespace Moneo.Tests.TaskManagementApiTests;

public class CompleteOrSkipTaskRequestHandlerTests : TaskManagementApiTestBase
{
    private static CompleteTaskRequestHandler GetHandler(MoneoTasksDbContext dbContext, TimeProvider timeProvider)
        => new(dbContext, timeProvider);

    [Fact]
    public async Task Handle_ReturnsSuccess_WhenTaskCompleted()
    {
        // Arrange
        var conversation = Fixture.CreateConversations().Single();
        var task = Fixture.CreateTasks(conversationId: conversation.Id).Single();
        var request = new CompleteOrSkipTaskRequest(task.Id, TaskCompletionType.Completed);
        var handler = GetHandler(DbContext, TimeProvider.Object);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ReturnsSuccess_WhenTaskSkipped()
    {
        // Arrange
        var conversation = Fixture.CreateConversations().Single();
        var task = Fixture.CreateTasks(conversationId: conversation.Id, active: true, name: "Task",
            description: "Description", timezone: "UTC", repeater: null, badger: null, dueDate: null).Single();
        task.CanBeSkipped = true;
        await DbContext.SaveChangesAsync();

        var request = new CompleteOrSkipTaskRequest(task.Id, TaskCompletionType.Skipped);
        var handler = GetHandler(DbContext, TimeProvider.Object);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ReturnsFailedResult_WhenTaskNotFound()
    {
        // Arrange
        var request = new CompleteOrSkipTaskRequest(0, TaskCompletionType.Completed); // Invalid task ID to trigger failure
        var handler = GetHandler(DbContext, TimeProvider.Object);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Task not found", result.Message);
    }

    [Fact]
    public async Task Handle_ReturnsFailedResult_WhenTaskIsInactive()
    {
        // Arrange
        var conversation = Fixture.CreateConversations().Single();
        var task = Fixture.CreateTasks(conversationId: conversation.Id, active: false).Single();
        var request = new CompleteOrSkipTaskRequest(task.Id, TaskCompletionType.Completed);
        var handler = GetHandler(DbContext, TimeProvider.Object);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Task is inactive", result.Message);
    }

    [Fact]
    public async Task Handle_ReturnsFailedResult_WhenCompletionTypeIsNone()
    {
        // Arrange
        var conversation = Fixture.CreateConversations().Single();
        var task = Fixture.CreateTasks(conversationId: conversation.Id).Single();
        var request = new CompleteOrSkipTaskRequest(task.Id, TaskCompletionType.None);
        var handler = GetHandler(DbContext, TimeProvider.Object);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Unknown completion type (must be Completed or Skipped)", result.Message);
    }

    [Fact]
    public async Task Handle_ReturnsFailedResult_WhenTaskCannotBeSkipped()
    {
        // Arrange
        var conversation = Fixture.CreateConversations().Single();
        var task = Fixture.CreateTasks(conversationId: conversation.Id, active: true, name: "Task",
            description: "Description", timezone: "UTC", repeater: null, badger: null, dueDate: null).Single();
        task.CanBeSkipped = false;
        await DbContext.SaveChangesAsync();

        var request = new CompleteOrSkipTaskRequest(task.Id, TaskCompletionType.Skipped);
        var handler = GetHandler(DbContext, TimeProvider.Object);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Task cannot be skipped", result.Message);
    }
}