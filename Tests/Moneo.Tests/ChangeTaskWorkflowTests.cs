using AutoFixture.AutoMoq;
using MediatR;
using Microsoft.Extensions.Logging;
using Moneo.Chat;
using Moneo.Chat.Commands;
using Moneo.Chat.Workflows.ChangeTask;
using Moneo.Chat.Workflows.CreateTask;
using Moneo.Obsolete.TaskManagement;
using Moneo.Obsolete.TaskManagement.Client.Models;
using Moneo.Obsolete.TaskManagement.Models;

namespace Moneo.Tests;

/// <summary>
/// This set of tests is for the ChangeTaskWorkflowManager and the ChangeTaskStateMachine
/// </summary>
public class ChangeTaskWorkflowTests
{
    // generate fixtures for the tests using Moq and Autofixture
    private readonly Fixture _fixture = new();
    private readonly Mock<IMediator> _mediator;
    private readonly Mock<ILogger<ChangeTaskWorkflowManager>> _logger;
    private readonly Mock<ITaskResourceManager> _taskResourceManager;
    private readonly Dictionary<long, List<MoneoTaskDto>> _tasks = new();
    private const long ChatId = 123456789;
    private const string ExistingTaskName = "Existing Task";
    private readonly DateTime _existingTaskCreatedDate = new(2022, 1, 1);

    public ChangeTaskWorkflowTests()
    {
        _fixture.Customize(new AutoMoqCustomization());
        _mediator = _fixture.Freeze<Mock<IMediator>>();
        _logger = _fixture.Freeze<Mock<ILogger<ChangeTaskWorkflowManager>>>();
        _taskResourceManager = _fixture.Freeze<Mock<ITaskResourceManager>>();

        _tasks[ChatId] =
        [
            new MoneoTaskDto
            {
                Name = ExistingTaskName,
                IsActive = true,
                Id = Guid.NewGuid().ToString(),
                Description = "This is a test task",
                ConversationId = ChatId,
                CompletedMessage = "Task completed",
                DueDates = [_fixture.Create<DateTime>()],
                Reminders = [],
                Created = _existingTaskCreatedDate,
                TimeZone = "Pacific Standard Time",
                LastUpdated = _existingTaskCreatedDate,
            }
        ];
        
        _taskResourceManager.Setup(x => x.GetTasksForUserAsync(It.IsAny<long>(), It.IsAny<MoneoTaskFilter>()))
            .ReturnsAsync((long chatId, MoneoTaskFilter filter) =>
            {
                _ = _tasks.TryGetValue(chatId, out var tasks);
                return new MoneoTaskResult<IEnumerable<MoneoTaskDto>>(true,
                    tasks ?? []);
            });
        _taskResourceManager
            .Setup(x => x.UpdateTaskAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<MoneoTaskDto>()))
            .ReturnsAsync((long chatId, string taskId, MoneoTaskDto taskDto) =>
            {
                if (!_tasks.TryGetValue(chatId, out var tasks))
                {
                    return new MoneoTaskResult(false, "Chat not found");
                }

                var task = tasks.FirstOrDefault(x => x.Id == taskId);

                if (task is null)
                {
                    return new MoneoTaskResult(false, "Task not found");
                }

                task.Name = taskDto.Name;
                task.Description = taskDto.Description;
                task.IsActive = taskDto.IsActive;
                task.DueDates = taskDto.DueDates;
                task.CompletedMessage = taskDto.CompletedMessage;
                task.Reminders = taskDto.Reminders;
                task.TimeZone = taskDto.TimeZone;
                task.LastUpdated = DateTime.UtcNow;

                return new MoneoTaskResult(true);
            });
    }
    
    [Fact]
    public async Task StartWorkflowAsync_WhenChatIdIsAlreadyInChatStates_ReturnsError()
    {
        // Arrange
        var workflowManager = _fixture.Build<ChangeTaskWorkflowManager>().Create();
        
        // start one workflow
        _ = await workflowManager.StartWorkflowAsync(ChatId, ExistingTaskName);
        
        // Act
        // try to start a second workflow
        var result = await workflowManager.StartWorkflowAsync(ChatId, _fixture.Create<string>());
        
        // Assert
        Assert.Equal(ResponseType.Text, result.ResponseType);
        Assert.Equal(ResultType.Error, result.Type);
        Assert.Equal("You cannot change a task while you're still creating or changing another one!", result.UserMessageText);
    }
    
    [Fact]
    public async Task StartWorkflowAsync_WhenNoTaskNameIsGiven_ReturnsError()
    {
        // Arrange
        var chatId = _fixture.Create<long>();
        var workflowManager = _fixture.Build<ChangeTaskWorkflowManager>().Create();
        
        // Act
        // try to start a second workflow
        var result = await workflowManager.StartWorkflowAsync(chatId, null);
        
        // Assert
        Assert.Equal(ResponseType.Text, result.ResponseType);
        Assert.Equal(ResultType.Error, result.Type);
        Assert.Equal("You have to tell me which one you want to change.", result.UserMessageText);
    }
    
    [Fact]
    public async Task StartWorkflowAsync_WhenTaskNameIsGiven_CreatesStateMachineAndAddsToChatStates()
    {
        // Arrange
        var workflowManager = _fixture.Build<ChangeTaskWorkflowManager>().Create();
        
        // Act
        var result = await workflowManager.StartWorkflowAsync(ChatId, ExistingTaskName);
        
        // Assert
        Assert.Equal(ResponseType.Menu, result.ResponseType);
        Assert.Equal(ResultType.NeedMoreInfo, result.Type);
        Assert.StartsWith("What would you like to change about the task?", result.UserMessageText);
    }

    [Theory]
    [InlineData("1", CreateOrUpdateTaskResponse.AskForNameResponse)]
    [InlineData(" 2 ", CreateOrUpdateTaskResponse.AskForDescriptionResponse)]
    [InlineData("1. Name", CreateOrUpdateTaskResponse.AskForNameResponse)]
    [InlineData("Name", CreateOrUpdateTaskResponse.AskForNameResponse)]
    [InlineData("2", CreateOrUpdateTaskResponse.AskForDescriptionResponse)]
    public async Task ContinueWorkflowAsync_WhenStateIsWaitingForUserDirection_HandlesMenuSelection(string userInput, string expectedText)
    {
        // Arrange
        var workflowManager = _fixture.Build<ChangeTaskWorkflowManager>().Create();
        
        _taskResourceManager.Setup(x => x.GetTasksForUserAsync(It.IsAny<long>(), It.IsAny<MoneoTaskFilter>()))
            .ReturnsAsync(new MoneoTaskResult<IEnumerable<MoneoTaskDto>>(true,
                new List<MoneoTaskDto>
                {
                    CreateTaskDto(ChatId, ExistingTaskName)
                }));
        
        // start the workflow
        _ = await workflowManager.StartWorkflowAsync(ChatId, ExistingTaskName);
        
        // Act
        var result = await workflowManager.ContinueWorkflowAsync(ChatId, userInput);
        
        // Assert
        Assert.Equal(ResponseType.Text, result.ResponseType);
        Assert.Equal(ResultType.NeedMoreInfo, result.Type);
        Assert.Equal(expectedText, result.UserMessageText);
    }
    
    [Theory]
    [InlineData("200000")]
    [InlineData("1. Lorem ipsum dolor")]
    [InlineData("Lorem ipsum")]
    [InlineData("Task")] // there are multiple options with "Task" in the name
    public async Task ContinueWorkflowAsync_WhenStateIsWaitingForUserDirection_ReturnsErrorMessageForInvalidSelection(string userInput)
    {
        // Arrange
        var workflowManager = _fixture.Build<ChangeTaskWorkflowManager>().Create();
        
        // start the workflow
        _ = await workflowManager.StartWorkflowAsync(ChatId, ExistingTaskName);
        
        // Act
        var result = await workflowManager.ContinueWorkflowAsync(ChatId, userInput);
        
        // Assert
        Assert.Equal(ResponseType.Text, result.ResponseType);
        Assert.Equal(ResultType.Error, result.Type);
    }

    [Fact]
    public async Task EndToEndWorkflow_CompletesSuccessfully()
    {
        var workflowManager = _fixture.Build<ChangeTaskWorkflowManager>().Create();
        var currentResponse = await workflowManager.StartWorkflowAsync(ChatId, ExistingTaskName);
        var currentStep = 1;

        var requests = new Dictionary<int, string>
        {
            { 1, "1" },
            { 2, "2" },
            { 3, "7" }
        };

        while (currentResponse.Type != ResultType.Error && currentResponse.Type != ResultType.WorkflowCompleted)
        {
            var responseText = currentResponse.UserMessageText switch
            {
                CreateOrUpdateTaskResponse.AskForNameResponse => "New Task Name",
                CreateOrUpdateTaskResponse.AskForDescriptionResponse => "New Task Description",
                { } s when s.StartsWith("What would you like to change about the task?") => requests[currentStep++],
                _ => throw new ArgumentOutOfRangeException()
            };
            
            currentResponse = await workflowManager.ContinueWorkflowAsync(ChatId, responseText);
        }
        
        Assert.Equal(ResultType.WorkflowCompleted, currentResponse.Type);
        // assert that the mediator sent a single UpdateTaskWorkflowCompletedEvent
        _mediator.Verify(x => x.Send(It.IsAny<ChangeTaskWorkflowCompletedEvent>(), default));
    }
    
    private MoneoTaskDto CreateTaskDto(long conversationId, string taskName)
    {
        return new MoneoTaskDto
        {
            Name = taskName,
            IsActive = true,
            Id = Guid.NewGuid().ToString(),
            Description = "This is a test task",
            ConversationId = conversationId,
            CompletedMessage = "Task completed",
            DueDates = [_fixture.Create<DateTime>()]
        };
    }
}