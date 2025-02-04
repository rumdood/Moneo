namespace Moneo.Moneo.Chat.Telegram.Api;

public static class ChatConstants
{
    public static class ErrorMessages
    {
        public const string UserMessageFormatInvalid = "User message format is invalid";
    }
    
    public static class Routes
    {
        public const string ReceiveFromUser = "receive";
        public const string StartAdapterRoute = "start";
        public const string StopAdapterRoute = "stop";
        public const string SendToUser = "send";
    }
}