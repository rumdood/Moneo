using Moneo.TaskManagement.Api.Features.GetTasks;
using Moneo.TaskManagement.ResourceAccess;

namespace Moneo.Tests.TaskManagementApiTests;

public class GetTasksByKeywordSearchRequestHandlerTests : TaskManagementApiTestBase
{
    private static GetTasksByKeywordSearchRequestHandler GetHandler(MoneoTasksDbContext dbContext)
        => new(dbContext);
    
    [Fact]
    public async Task Handle_ReturnsTasks_WhenKeywordsMatch()
    {
        // Arrange
        var conversation = Fixture.CreateConversations().Single();
        var task = Fixture
            .CreateTasks(
                conversationId: conversation.Id, 
                name: "foo foo foo"
            )
            .Single();
        
        _ = Fixture.CreateTasks(
            conversationId: conversation.Id, 
            name: "bar bar bar"
        );
        
        var request = new GetTasksByKeywordSearchRequest(conversation.Id, "foo");

        // Act
        var result = await GetHandler(DbContext).Handle(request, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.Data);
        Assert.Single(result.Data.Data);
        Assert.Equal(task.Name, result.Data.Data[0].Name);
    }

    [Fact]
    public async Task Handle_ReturnsEmpty_WhenNoTasksMatch()
    {
        // Arrange
        var conversation = Fixture.CreateConversations().Single();
        var task = Fixture.CreateTasks(
            1, 
            conversation.Id,
            name: "123456789")
            .Single();
        
        var request = new GetTasksByKeywordSearchRequest(1, "nonexistent");

        // Act
        var result = await GetHandler(DbContext).Handle(request, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("No tasks found matching the search criteria", result.Message);
    }
}