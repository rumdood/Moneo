using MediatR;
using Moneo.Chat.Commands;
using Moq;
using Moneo.Chat.UserRequests;
using Moneo.TaskManagement;

namespace Moneo.Chat.Tests;

public class UnitTest1
{
    [Fact]
    public async Task CanCreateCompleteRequestWithoutArgs()
    {
        // setup
        var mgr = new Mock<ITaskResourceManager>();
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
        
        long conversationId = 1;
        var userString = "/complete";
        var context = CommandContext.Get(1, ChatState.Waiting, userString);
        
        var parts = userString.Split(' ');
        context.CommandKey = parts[0].ToLowerInvariant();
        context.Args = parts[1..];

        var request = UserRequestFactory.GetUserRequest(context);

        Assert.IsType<CompleteTaskRequest>(request);

        var handler = new CompleteTaskRequestHandler(mediator.Object, mgr.Object);
        var result = await handler.Handle(request as CompleteTaskRequest, CancellationToken.None);
        
        Assert.NotNull(result);
        Assert.Equal(ResponseType.Menu, result!.ResponseType);
    }
}