using MediatR;
using Moneo.Chat.CommandRegistration;
using Moneo.Chat.Commands;
using Moq;
using Moneo.Chat.Workflows;
using Moneo.TaskManagement.Contracts;

namespace Moneo.Chat.Tests;

public class UnitTest1
{
    [Fact]
    public async Task CanCreateCompleteRequestWithoutArgs()
    {
        // setup
        var mgr = new Mock<ITaskManagerClient>();
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<IRequest<MoneoCommandResult>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IRequest<MoneoCommandResult> request, CancellationToken token) =>
                new MoneoCommandResult
                {
                    ResponseType = ResponseType.Menu,
                    Type = ResultType.WorkflowCompleted,
                    UserMessageText = "Please select a task:",
                    MenuOptions = ["Option 1", "Option 2"]
                });

        CommandRegistrar.RegisterCommands(new MoneoChatCommandConfiguration
        {
            UserRequestsToRegister = { typeof(CompleteTaskRequest) }
        });
        
        long conversationId = 1;
        var userString = "/complete";
        var context = CommandContextFactory.BuildCommandContext(1, ChatState.Waiting, userString);

        var request = UserRequestFactory.GetUserRequest(context) as CompleteTaskRequest;

        Assert.IsType<CompleteTaskRequest>(request);
        
        var manager = new CompleteTaskWorkflowManager(mediator.Object, mgr.Object);
        var result =
            await manager.StartWorkflowAsync(request.ConversationId, request.TaskName, CompleteTaskOption.Complete);
        
        Assert.NotNull(result);
        Assert.Equal(ResponseType.Menu, result!.ResponseType);
    }
}