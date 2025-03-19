using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moneo.Chat;
using Moneo.Chat.Models;
using Moneo.Chat.Telegram;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Moneo.Tests;

public class TelegramTests
{
    [Fact]
    public void ValidUpdateJson_Can_Deserialize_To_Update()
    {
        var json =
            "{\"update_id\":492338096,\n\"message\":{\"message_id\":14997,\"from\":{\"id\":122243374,\"is_bot\":false,\"first_name\":\"RumDood\",\"username\":\"rumdood\",\"language_code\":\"en\"},\"chat\":{\"id\":122243374,\"first_name\":\"RumDood\",\"username\":\"rumdood\",\"type\":\"private\"},\"date\":1740199466,\"text\":\"test from tg again\"}}";
        
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    
        var update = JsonSerializer.Deserialize<Update>(json, options);
        
        Assert.NotNull(update);
        Assert.NotNull(update.Message);
    }
    
    [Fact]
    public async Task ReceiveMessage_WhenJsonIsValid_DoesNotThrow()
    {
        var botClient = new Mock<ITelegramBotClient>();
        var convoManager = new Mock<IChatManager>();
        convoManager.Setup(c => c.ProcessUserMessageAsync(It.IsAny<UserMessage>()))
            .Returns(Task.CompletedTask);
        var options = new TelegramChatAdapterOptions();
        var logger = new Mock<ILogger<TelegramChatAdapter>>();

        var adapter = new TelegramChatAdapter(options, convoManager.Object, logger.Object, botClient.Object);
        
        var json =
            "{\"update_id\":492338096,\n\n      \"message\":{\"message_id\":14997,\"from\":{\"id\":122243374,\"is_bot\":false,\"first_name\":\"RumDood\",\"username\":\"rumdood\",\"language_code\":\"en\"},\"chat\":{\"id\":122243374,\"first_name\":\"RumDood\",\"username\":\"rumdood\",\"type\":\"private\"},\"date\":1740199466,\"text\":\"test from tg again\"}}";
    }
}