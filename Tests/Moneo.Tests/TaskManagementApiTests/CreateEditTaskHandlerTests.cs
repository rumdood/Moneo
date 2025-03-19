using Moneo.Common;
using Moneo.TaskManagement.Contracts.Models;
using Moneo.TaskManagement.Features.CreateEditTask;
using Moneo.TaskManagement.ResourceAccess;
using Moneo.TaskManagement.ResourceAccess.Entities;
using Xunit;

namespace Moneo.Tests.TaskManagementApiTests;

public class CreateEditTaskHandlerTests : TaskManagementApiTestBase
{
    private static CreateEditTaskHandler GetHandler(MoneoTasksDbContext dbContext, TimeProvider timeProvider)
        => new(dbContext, timeProvider);

    [Fact]
    public async Task Handle_ReturnsSuccess_WhenTaskCreated()
    {
        // Arrange
        var conversation = Fixture.CreateConversations().Single();
        var editDto = new CreateEditTaskDto(
            Name: "New Task",
            Description: "Description",
            IsActive: true,
            CompletedMessages: ["fee", "fie", "foe", "fum"],
            CanBeSkipped: false,
            SkippedMessages: [],
            Timezone: "UTC",
            DueOn: TimeProvider.Object.GetUtcNow().AddHours(5).UtcDateTime,
            Repeater: null,
            Badger: null
        );
        var request = new CreateEditTaskRequest(editDto, ConversationId: conversation.Id);
        var handler = GetHandler(DbContext, TimeProvider.Object);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEqual(0, result.Data);
    }

    [Fact]
    public async Task Handle_ReturnsFailure_WhenTaskHasNoCompletedMessages()
    {
        // Arrange
        var conversation = Fixture.CreateConversations().Single();
        var editDto = new CreateEditTaskDto(
            Name: "New Task",
            Description: "Description",
            IsActive: true,
            CompletedMessages: [],
            CanBeSkipped: false,
            SkippedMessages: [],
            Timezone: "UTC",
            DueOn: null,
            Repeater: null,
            Badger: null
        );
        
        var request = new CreateEditTaskRequest(editDto, ConversationId: conversation.Id);
        var handler = GetHandler(DbContext, TimeProvider.Object);
        
        // Act
        var result = await handler.Handle(request, CancellationToken.None);
        
        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Exception);
    }

    [Fact]
    public async Task Handle_ReturnsSuccess_WhenTaskUpdated()
    {
        // Arrange
        var conversation = Fixture.CreateConversations().Single();
        var task = Fixture.CreateTasks(conversationId: conversation.Id).Single();
        var editDto = new CreateEditTaskDto(
            Name: "Updated Task",
            Description: "Description",
            IsActive: true,
            CompletedMessages: [],
            CanBeSkipped: false,
            SkippedMessages: [],
            Timezone: "UTC",
            DueOn: null,
            Repeater: null,
            Badger: null
        );
        var request = new CreateEditTaskRequest(editDto, task.Id, conversation.Id);
        var handler = GetHandler(DbContext, TimeProvider.Object);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(task.Id, result.Data);
    }

    [Fact]
    public async Task Handle_ReturnsFailedResult_WhenTaskNotFound()
    {
        // Arrange
        var conversation = Fixture.CreateConversations().Single();
        var editDto = new CreateEditTaskDto(
            Name: "Nonexistent Task",
            Description: "Description",
            IsActive: true,
            CompletedMessages: [],
            CanBeSkipped: false,
            SkippedMessages: [],
            Timezone: "UTC",
            DueOn: null,
            Repeater: null,
            Badger: null
        );
        var request = new CreateEditTaskRequest(editDto, 0, conversation.Id); // Invalid task ID to trigger failure
        var handler = GetHandler(DbContext, TimeProvider.Object);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Task not found", result.Message);
    }

    [Fact]
    public async Task Handle_ReturnsFailedResult_WhenConversationNotFound()
    {
        // Arrange
        var editDto = new CreateEditTaskDto(
            Name: "New Task",
            Description: "Description",
            IsActive: true,
            CompletedMessages: [],
            CanBeSkipped: false,
            SkippedMessages: [],
            Timezone: "UTC",
            DueOn: null,
            Repeater: null,
            Badger: null
        );
        var request = new CreateEditTaskRequest(editDto, null, 0); // Invalid conversation ID to trigger failure
        var handler = GetHandler(DbContext, TimeProvider.Object);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Either TaskId or ConversationId must be provided", result.Message);
    }

    [Fact]
    public async Task Handle_ReturnsFailedResult_WhenTaskNameAlreadyExists()
    {
        // Arrange
        var conversation = Fixture.CreateConversations().Single();
        var existingTask = Fixture.CreateTasks(conversationId: conversation.Id).Single();
        var editDto = new CreateEditTaskDto(
            Name: existingTask.Name,
            Description: "Description",
            IsActive: true,
            CompletedMessages: [],
            CanBeSkipped: false,
            SkippedMessages: [],
            Timezone: "UTC",
            DueOn: null,
            Repeater: null,
            Badger: null
        );
        var request = new CreateEditTaskRequest(editDto, null, conversation.Id);
        var handler = GetHandler(DbContext, TimeProvider.Object);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("A task with that name already exists", result.Message);
    }
}