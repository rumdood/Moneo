using Moneo.Chat;

namespace Moneo.Tests;

public class InMemoryChatStateRepositoryTest
{
    private readonly IChatStateRepository _chatStateRepository = new InMemoryChatStateRepository();

    [Fact]
    public async Task UpdateChatStateAsync_WhenChatStateIsNotSet_ShouldUpdateChatState()
    {
        // Arrange
        const int chatId = 1;
        var chatState = ChatState.CreateTask;

        // Act
        await _chatStateRepository.UpdateChatStateAsync(chatId, chatState);

        // Assert
        var result = await _chatStateRepository.GetChatStateAsync(chatId);
        Assert.Equal(chatState, result);
    }
    
    [Fact]
    public async Task UpdateChatStateAsync_WhenChatStateIsAlreadySet_ShouldUpdateChatState()
    {
        // Arrange
        const int chatId = 1;
        var chatState = ChatState.CreateTask;
        var newChatState = ChatState.ChangeTask;
        
        // Act
        await _chatStateRepository.UpdateChatStateAsync(chatId, chatState);
        await _chatStateRepository.UpdateChatStateAsync(chatId, newChatState);

        // Assert
        var result = await _chatStateRepository.GetChatStateAsync(chatId);
        Assert.Equal(newChatState, result);
    }
    
    [Fact]
    public async Task RevertChatStateAsync_WhenChatStateIsSet_ShouldRevertChatState()
    {
        // Arrange
        const int chatId = 1;
        var chatState = ChatState.CreateTask;
        var newChatState = ChatState.ChangeTask;
        
        // Act
        await _chatStateRepository.UpdateChatStateAsync(chatId, chatState);
        await _chatStateRepository.UpdateChatStateAsync(chatId, newChatState);
        await _chatStateRepository.RevertChatStateAsync(chatId);

        // Assert
        var result = await _chatStateRepository.GetChatStateAsync(chatId);
        Assert.Equal(chatState, result);
    }
    
    [Fact]
    public async Task RevertChatStateAsync_WhenChatStateIsWaiting_ShouldNotRevertChatState()
    {
        // Arrange
        const int chatId = 1;
        
        // Act
        await _chatStateRepository.UpdateChatStateAsync(chatId, ChatState.CreateTask);
        await _chatStateRepository.UpdateChatStateAsync(chatId, ChatState.Waiting);
        await _chatStateRepository.RevertChatStateAsync(chatId);

        // Assert
        var result = await _chatStateRepository.GetChatStateAsync(chatId);
        Assert.Equal(ChatState.Waiting, result);
    }

    [Fact]
    public async Task RevertChatStateAsync_WhenCalledMultipleTimes_Works()
    {
        await _chatStateRepository.UpdateChatStateAsync(1, ChatState.ChangeTask);
        await _chatStateRepository.UpdateChatStateAsync(1, ChatState.CreateCron);
        await _chatStateRepository.RevertChatStateAsync(1);
        await _chatStateRepository.RevertChatStateAsync(1);
        
        var result = await _chatStateRepository.GetChatStateAsync(1);
        Assert.Equal(ChatState.Waiting, result);
    }
}