using Moneo.Common;
using Moneo.TaskManagement.Contracts.Models;
using Moneo.TaskManagement.Features.GetTasks;

namespace Moneo.Tests.TaskManagementApiTests;

public class GetTasksByFilterRequestHandlerTests : TaskManagementApiTestBase
{
    [Fact]
    public async Task Handle_ReturnsPagedList_WhenTasksExist()
    {
        // Arrange
        var conversation = Fixture.CreateConversations(1).Single();
        var tasks = Fixture.CreateTasks(2, conversation.Id);
        var filter = TaskFilter.ForConversation(conversation.Id).WithActive(true);
        var pagingOptions = new PageOptions(0, 10);
        var request = new GetTasksByFilterRequest(filter, pagingOptions);
        var handler = new GetTasksByFilterRequestHandler(DbContext);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.Data?.Count);
    }

    [Fact]
    public async Task Handle_ReturnsEmptyList_WhenNoTasksExistForFilter()
    {
        // Arrange
        var conversations = Fixture.CreateConversations(3).ToArray();
        
        _ = Fixture.CreateTasks(
            5, 
            conversations[0].Id);
        
        var filter = TaskFilter.ForConversation(conversations[1].Id).WithActive(true);
        var pagingOptions = new PageOptions(0, 10);
        var request = new GetTasksByFilterRequest(filter, pagingOptions);
        var handler = new GetTasksByFilterRequestHandler(DbContext);
        
        // Act
        var result = await handler.Handle(request, CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.Data);
        Assert.Empty(result.Data.Data);
    }

    [Fact]
    public async Task Handle_ReturnsFailedResult_WhenExceptionThrown()
    {
        // Arrange
        var pagingOptions = new PageOptions(0, 10);
        var request = new GetTasksByFilterRequest(null, pagingOptions);
        
        var conversation = Fixture.CreateConversations(1).Single();
        var tasks = Fixture.CreateTasks(2, conversation.Id);
        var handler = new GetTasksByFilterRequestHandler(DbContext);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Exception);
    }
}