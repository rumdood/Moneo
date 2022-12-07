using Telegram.Bot.Types.ReplyMarkups;

namespace Moneo.Functions.Bot
{
    public record class BotResponse(
        string MessageText,
        long ChatId = default,
        InlineKeyboardMarkup Inline = default,
        bool? IsMarkdown = default,
        bool? DisablePagePreview = default);
}
