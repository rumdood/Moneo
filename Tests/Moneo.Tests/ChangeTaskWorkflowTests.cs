using AutoFixture.AutoMoq;
using MediatR;
using Microsoft.Extensions.Logging;
using Moneo.Chat.Commands;
using Moneo.Common;
using Moneo.Hosts.Chat.Api.Tasks;
using Moneo.TaskManagement.Contracts;
using Moneo.TaskManagement.Contracts.Models;
using Moneo.TaskManagement.Workflows;
using Moneo.TaskManagement.Workflows.ChangeTask;
using Moneo.TaskManagement.Workflows.CreateTask;

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
    private readonly Mock<ITaskManagerClient> _taskResourceManager;
    private readonly Dictionary<long, List<MoneoTaskDto>> _tasksByConversationId = new();
    private readonly Dictionary<long, MoneoTaskDto> _tasksById = new();
    private const long ChatId = 123456789;
    private const long ExistingTaskId = 987654321;
    private const long UserId = 1234567890;
    private const string ExistingTaskName = "Existing Task";
    private readonly DateTime _existingTaskCreatedDate = new(2022, 1, 1);

    public ChangeTaskWorkflowTests()
    {
        _fixture.Customize(new AutoMoqCustomization());
        _mediator = _fixture.Freeze<Mock<IMediator>>();
        _logger = _fixture.Freeze<Mock<ILogger<ChangeTaskWorkflowManager>>>();
        _taskResourceManager = _fixture.Freeze<Mock<ITaskManagerClient>>();
        
        var taskCreateOrChangeStateMachineRepository = new TaskCreateOrChangeStateMachineRepository();
        _fixture.Inject<IWorkflowWithTaskDraftStateMachineRepository>(taskCreateOrChangeStateMachineRepository);

        _tasksByConversationId[ChatId] =
        [
            _fixture.Build<MoneoTaskDto>()
                .With(x => x.Name, ExistingTaskName)
                .With(x => x.IsActive, true)
                .With(x => x.Id, ExistingTaskId)
                .With(x => x.Description, "This is a test task")
                .With(x => x.CompletedMessages, ["Task completed"])
                .With(x => x.DueOn, _fixture.Create<DateTime>())
                .With(x => x.Timezone, "Pacific Standard Time")
                .Without(x => x.Badger)
                .Without(x => x.Repeater)
                .Create()
        ];
        
        _tasksById[ExistingTaskId] = _tasksByConversationId[ChatId].First();
        
        _taskResourceManager.Setup(x => x.GetTasksForConversationAsync(It.IsAny<long>(), It.IsAny<PageOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((long chatId, PageOptions pgOpt, CancellationToken _) =>
            {
                _tasksByConversationId.TryGetValue(chatId, out var tasks);
                return MoneoResult<PagedList<MoneoTaskDto>>.Success(new PagedList<MoneoTaskDto>
                {
                    Data = tasks ?? [],
                    Page = 0,
                    PageSize = pgOpt.PageSize,
                    TotalCount = tasks?.Count ?? 0
                });
            });
        
        _taskResourceManager
            .Setup(x => x.UpdateTaskAsync(It.IsAny<long>(), It.IsAny<CreateEditTaskDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((long taskId, CreateEditTaskDto dto, CancellationToken _) =>
            {
                if (!_tasksById.TryGetValue(taskId, out var task))
                {
                    return MoneoResult<MoneoTaskDto>.TaskNotFound("Task not found");
                }

                var result = MoneoResult.Success();
                return result;
            });
        
        _taskResourceManager.Setup(x => x.GetTasksByKeywordSearchAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<PageOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((long _, string keyword, PageOptions pgOpt, CancellationToken _) => MoneoResult<PagedList<MoneoTaskDto>>.Success(new PagedList<MoneoTaskDto>
            {
                Data = [_tasksById[ExistingTaskId]],
                Page = 0,
                PageSize = pgOpt.PageSize,
                TotalCount = 1
            }));
    }
    
    [Fact]
    public async Task StartWorkflowAsync_WhenChatIdIsAlreadyInChatStates_ReturnsError()
    {
        // Arrange
        var workflowManager = _fixture.Build<ChangeTaskWorkflowManager>().Create();
        var context = _fixture.GetCommandContext(ChatId, _fixture.GetUser(UserId));
        
        // start one workflow
        _ = await workflowManager.StartWorkflowAsync(context, ExistingTaskName);
        
        // Act
        // try to start a second workflow
        var result = await workflowManager.StartWorkflowAsync(context, _fixture.Create<string>());
        
        // Assert
        Assert.Equal(ResponseType.Text, result.ResponseType);
        Assert.Equal(ResultType.Error, result.Type);
        Assert.Equal("You cannot change a task while you're still creating or changing another one!", result.UserMessageText);
    }
    
    [Fact]
    public async Task StartWorkflowAsync_WhenNoTaskNameIsGiven_ReturnsError()
    {
        // Arrange
        var context = _fixture.GetCommandContext();
        var workflowManager = _fixture.Build<ChangeTaskWorkflowManager>().Create();
        
        // Act
        // try to start a second workflow
        var result = await workflowManager.StartWorkflowAsync(context, null);
        
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
        var context = _fixture.GetCommandContext(ChatId, _fixture.GetUser(UserId));
        
        // Act
        var result = await workflowManager.StartWorkflowAsync(context, ExistingTaskName);
        
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
        var context = _fixture.GetCommandContext(ChatId, _fixture.GetUser(UserId));
        
        // start the workflow
        _ = await workflowManager.StartWorkflowAsync(context, ExistingTaskName);
        
        // Act
        var result = await workflowManager.ContinueWorkflowAsync(context, userInput);
        
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
        var context = _fixture.GetCommandContext(ChatId, _fixture.GetUser(UserId));
        
        // start the workflow
        _ = await workflowManager.StartWorkflowAsync(context, ExistingTaskName);
        
        // Act
        var result = await workflowManager.ContinueWorkflowAsync(context, userInput);
        
        // Assert
        Assert.Equal(ResponseType.Text, result.ResponseType);
        Assert.Equal(ResultType.Error, result.Type);
    }

    [Fact]
    public async Task EndToEndWorkflow_CompletesSuccessfully()
    {
        var workflowManager = _fixture.Build<ChangeTaskWorkflowManager>().Create();
        var context = _fixture.GetCommandContext(ChatId, _fixture.GetUser(UserId), commandText: ExistingTaskName);
        var currentResponse = await workflowManager.StartWorkflowAsync(context, ExistingTaskName);
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
            
            context.Args = [responseText];
            
            currentResponse = await workflowManager.ContinueWorkflowAsync(context, responseText);
        }
        
        Assert.Equal(ResultType.WorkflowCompleted, currentResponse.Type);
        // assert that the mediator sent a single UpdateTaskWorkflowCompletedEvent
        _mediator.Verify(x => x.Send(It.IsAny<ChangeTaskWorkflowCompletedEvent>(), default));
    }
    
    private MoneoTaskDto CreateTaskDto(string taskName, long? taskId = null)
    {
        return new MoneoTaskDto
        {
            Name = taskName,
            IsActive = true,
            Id = taskId ?? _fixture.Create<long>(),
            Description = "This is a test task",
            CompletedMessages = ["Task completed"],
            DueOn = _fixture.Create<DateTimeOffset>(),
        };
    }
}