using Newtonsoft.Json;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Moneo.Functions.Bot;

public class BotService
{
    public static Task<User> GetBot(string token)
    {
        var client = new TelegramBotClient(token);
        return client.GetMeAsync();
    }

    public Task SetupWebhook()
    {
        var client = new TelegramBotClient(MoneoConfiguration.TelegramBotToken);
        var uri = MoneoConfiguration.BotUri;
        var key = MoneoConfiguration.BotClientId;

        return client.SetWebhookAsync($"https://{uri}/api/webhook?code={key}");
    }

    public Task RemoveWebhook()
    {
        var client = new TelegramBotClient(MoneoConfiguration.TelegramBotToken);
        return client.DeleteWebhookAsync();
    }

    internal Task AnswerCallback(string callbackId)
    {
        var client = new TelegramBotClient(MoneoConfiguration.TelegramBotToken);
        return client.AnswerCallbackQueryAsync(callbackId);
    }

    internal Task SendWaiting(long chatId)
    {
        var client = new TelegramBotClient(MoneoConfiguration.TelegramBotToken);
        return client.SendChatActionAsync(chatId, ChatAction.Typing);
    }

    internal Task SendResponse(BotResponse response)
    {
        var client = new TelegramBotClient(MoneoConfiguration.TelegramBotToken);
        return client.SendTextMessageAsync(
            chatId: response.ChatId,
            text: response.MessageText,
            parseMode: response.IsMarkdown.HasValue && response.IsMarkdown.Value ? Telegram.Bot.Types.Enums.ParseMode.MarkdownV2 : null,
            disableWebPagePreview: true,
            replyMarkup: response.Inline);
    }

    internal static Update GetTelegramUpdateFromJson(string updateJson)
        => JsonConvert.DeserializeObject<Update>(updateJson);
}
