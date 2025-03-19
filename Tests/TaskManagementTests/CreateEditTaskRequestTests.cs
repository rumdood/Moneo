using Microsoft.EntityFrameworkCore;
using Moneo.TaskManagement.Contracts.Models;
using Moneo.TaskManagement.DomainEvents;
using Moneo.TaskManagement.Features.CreateEditTask;

namespace TaskManagementTests;

public class CreateEditTaskRequestTests
{
    private readonly TestFixture _fixture;
    private readonly CreateEditTaskHandler _handler;

    public CreateEditTaskRequestTests()
    {
        _fixture = new TestFixture();
        _handler = new CreateEditTaskHandler(_fixture.DbContext, _fixture.TimeProvider);
    }

    [Fact]
    public async Task Handle_CreateNewTask_ReturnsSuccess()
    {
        // Arrange
        _fixture.ResetDbContext();
        var existingData = _fixture.InitConversationWithTasks([]);

        var request = new CreateEditTaskRequest(new CreateEditTaskDto(
            "New Task",
            "Task Description",
            true,
            ["Completed"],
            true,
            ["Skipped"],
            "Pacific",
            new DateTimeOffset(new DateTime(2025, 01, 01, 11, 15, 0)),
            null,
            null
        ), null, existingData.Conversation.Id);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEqual(0, result.Data);
        
        var lastEvent = _fixture.DomainEvents.Pop();
        Assert.IsType<TaskCreatedOrUpdated>(lastEvent);
    }

    [Fact]
    public async Task Handle_EditExistingTask_ReturnsSuccess()
    {
        // Arrange
        _fixture.ResetDbContext();
        var existingData = _fixture.InitConversationWithTasks([
            new TaskEntry("Existing Task", "Existing Task Description")
        ]);
        
        var existingTask = _fixture.DbContext.Tasks.AsNoTracking().Single(t => t.Name == "Existing Task");

        var request = new CreateEditTaskRequest(new CreateEditTaskDto(
            "Existing Task",
            "Task Description",
            true,
            ["Completed"],
            true,
            ["Skipped"],
            "Pacific",
            new DateTimeOffset(new DateTime(2025, 01, 01, 11, 15, 0)),
            null,
            null
        ), existingTask.Id);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(existingTask.Id, result.Data);
        
        var lastEvent = _fixture.DomainEvents.Pop();
        Assert.IsType<TaskCreatedOrUpdated>(lastEvent);
    }

    [Fact]
    public async Task Handle_MissingConversationId_ReturnsBadRequest()
    {
        // Arrange
        _fixture.ResetDbContext();
        var request = new CreateEditTaskRequest(new CreateEditTaskDto(
            "New Task",
            "Task Description",
            true,
            ["Completed"],
            true,
            ["Skipped"],
            "Pacific",
            new DateTimeOffset(new DateTime(2025, 01, 01, 11, 15, 0)),
            null,
            null
        ));

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Either TaskId or ConversationId must be provided", result.Message);
    }

    [Fact]
    public async Task Handle_DuplicateTaskName_ReturnsAlreadyExists()
    {
        // Arrange
        _fixture.ResetDbContext();
        var existingData = _fixture.InitConversationWithTasks([
            new TaskEntry("Duplicate Task", "Task Description")
        ]);

        var request = new CreateEditTaskRequest(new CreateEditTaskDto(
            "Duplicate Task",
            "Task Description",
            true,
            ["Completed"],
            true,
            ["Skipped"],
            "Pacific",
            new DateTimeOffset(new DateTime(2025, 01, 01, 11, 15, 0)),
            null,
            null
        ), null, existingData.Conversation.Id);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("A task with that name already exists", result.Message);
    }
}