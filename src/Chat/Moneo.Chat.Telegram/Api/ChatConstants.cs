namespace Moneo.Chat.Telegram.Api;

public static class ChatConstants
{
    public static class ErrorMessages
    {
        public const string UserMessageFormatInvalid = "User message format is invalid";
    }
    
    public static class Routes
    {
        private const string NotifyRoutePrefix = "api/notify";
        private const string TelegramRoutePrefix = "api/telegram";
        public const string GetStatusRoute = $"{TelegramRoutePrefix}/status";
        public const string ReceiveFromUser = $"{NotifyRoutePrefix}/receive";
        public const string StartAdapter = $"{TelegramRoutePrefix}/start";
        public const string StopAdapter = $"{TelegramRoutePrefix}/stop";
        public const string ConfigureAdapterRoute = $"{TelegramRoutePrefix}/configure";
        public const string SendTextToUser = $"{NotifyRoutePrefix}/send/text";
        public const string SendGifToUser = $"{NotifyRoutePrefix}/send/gif";
    }
}