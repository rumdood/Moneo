using Moneo.Common;
using Moneo.TaskManagement.Features.GetTasks;
using Moneo.TaskManagement.ResourceAccess;

namespace Moneo.Tests.TaskManagementApiTests;

public class GetTasksForConversationRequestHandlerTests : TaskManagementApiTestBase
{
    private static GetTasksForConversationRequestHandler GetHandler(MoneoTasksDbContext dbContext)
        => new(dbContext);

    [Fact]
    public async Task Handle_ReturnsPagedList_WhenTasksExist()
    {
        // Arrange
        var conversation = Fixture.CreateConversations(1).Single();
        var tasks = Fixture.CreateTasks(2, conversation.Id);
        var pagingOptions = new PageOptions(0, 10);
        var request = new GetTasksForConversationRequest(conversation.Id, pagingOptions);
        var handler = GetHandler(DbContext);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.Data?.Count);
    }

    [Fact]
    public async Task Handle_ReturnsEmptyList_WhenNoTasksExist()
    {
        // Arrange
        var conversation = Fixture.CreateConversations(1).Single();
        var pagingOptions = new PageOptions(0, 10);
        var request = new GetTasksForConversationRequest(conversation.Id, pagingOptions);
        var handler = GetHandler(DbContext);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Empty(result.Data.Data);
    }

    [Fact]
    public async Task Handle_ReturnsFailedResult_WhenExceptionThrown()
    {
        // Arrange
        var pagingOptions = new PageOptions(0, 10);
        var request = new GetTasksForConversationRequest(0, pagingOptions); // Invalid conversation ID to trigger exception
        var handler = GetHandler(DbContext);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Message);
        Assert.Equal("Conversation not found", result.Message);
    }

    [Fact]
    public async Task Handle_ReturnsSingleTask_WhenOneTaskExists()
    {
        // Arrange
        var conversation = Fixture.CreateConversations(1).Single();
        var task = Fixture.CreateTasks(1, conversation.Id).Single();
        var pagingOptions = new PageOptions(0, 10);
        var request = new GetTasksForConversationRequest(conversation.Id, pagingOptions);
        var handler = GetHandler(DbContext);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data.Data);
        Assert.Equal(task.Name, result.Data.Data[0].Name);
    }

    [Fact]
    public async Task Handle_ReturnsMultipleTasks_WhenMultipleTasksExist()
    {
        // Arrange
        var conversation = Fixture.CreateConversations(1).Single();
        var tasks = Fixture.CreateTasks(5, conversation.Id);
        var pagingOptions = new PageOptions(0, 10);
        var request = new GetTasksForConversationRequest(conversation.Id, pagingOptions);
        var handler = GetHandler(DbContext);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(5, result.Data.Data?.Count);
    }
}