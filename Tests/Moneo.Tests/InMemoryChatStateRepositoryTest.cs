using Microsoft.Extensions.Logging.Abstractions;
using Moneo.Chat;

namespace Moneo.Tests;

public class InMemoryChatStateRepositoryTest
{
    private readonly IChatStateRepository _chatStateRepository =
        new InMemoryChatStateRepository(new NullLogger<InMemoryChatStateRepository>());
    private readonly Fixture _fixture = new Fixture();

    [Fact]
    public async Task UpdateChatStateAsync_WhenChatStateIsNotSet_ShouldUpdateChatState()
    {
        // Arrange
        var context = _fixture.GetCommandContext();
        
        var chatState = ChatState.CreateTask;

        // Act
        await _chatStateRepository.UpdateChatStateAsync(context.ConversationId, context.User!.Id, chatState);

        // Assert
        var result = await _chatStateRepository.GetChatStateAsync(context.ConversationId, context.User!.Id);
        Assert.Equal(chatState, result);
    }
    
    [Fact]
    public async Task UpdateChatStateAsync_WhenChatStateIsAlreadySet_ShouldUpdateChatState()
    {
        // Arrange
        var context = _fixture.GetCommandContext();
        var chatState = ChatState.CreateTask;
        var newChatState = ChatState.ChangeTask;
        
        // Act
        await _chatStateRepository.UpdateChatStateAsync(context.ConversationId, context.User!.Id, chatState);
        await _chatStateRepository.UpdateChatStateAsync(context.ConversationId, context.User!.Id, newChatState);

        // Assert
        var result = await _chatStateRepository.GetChatStateAsync(context.ConversationId, context.User!.Id);
        Assert.Equal(newChatState, result);
    }
    
    [Fact]
    public async Task RevertChatStateAsync_WhenChatStateIsSet_ShouldRevertChatState()
    {
        // Arrange
        var context = _fixture.GetCommandContext();
        var chatState = ChatState.CreateTask;
        var newChatState = ChatState.ChangeTask;
        
        // Act
        await _chatStateRepository.UpdateChatStateAsync(context.ConversationId, context.User!.Id, chatState);
        await _chatStateRepository.UpdateChatStateAsync(context.ConversationId, context.User!.Id, newChatState);
        await _chatStateRepository.RevertChatStateAsync(context.ConversationId, context.User!.Id);

        // Assert
        var result = await _chatStateRepository.GetChatStateAsync(context.ConversationId, context.User!.Id);
        Assert.Equal(chatState, result);
    }
    
    [Fact]
    public async Task RevertChatStateAsync_WhenChatStateIsWaiting_ShouldNotRevertChatState()
    {
        // Arrange
        var context = _fixture.GetCommandContext();
        
        // Act
        await _chatStateRepository.UpdateChatStateAsync(context.ConversationId, context.User!.Id, ChatState.CreateTask);
        await _chatStateRepository.UpdateChatStateAsync(context.ConversationId, context.User!.Id, ChatState.Waiting);
        await _chatStateRepository.RevertChatStateAsync(context.ConversationId, context.User!.Id);

        // Assert
        var result = await _chatStateRepository.GetChatStateAsync(context.ConversationId, context.User!.Id);
        Assert.Equal(ChatState.Waiting, result);
    }

    [Fact]
    public async Task RevertChatStateAsync_WhenCalledMultipleTimes_Works()
    {
        var context = _fixture.GetCommandContext();
        await _chatStateRepository.UpdateChatStateAsync(context.ConversationId, context.User!.Id, ChatState.ChangeTask);
        await _chatStateRepository.UpdateChatStateAsync(context.ConversationId, context.User!.Id, ChatState.CreateCron);
        await _chatStateRepository.RevertChatStateAsync(context.ConversationId, context.User!.Id);
        await _chatStateRepository.RevertChatStateAsync(context.ConversationId, context.User!.Id);
        
        var result = await _chatStateRepository.GetChatStateAsync(context.ConversationId, context.User!.Id);
        Assert.Equal(ChatState.Waiting, result);
    }
}