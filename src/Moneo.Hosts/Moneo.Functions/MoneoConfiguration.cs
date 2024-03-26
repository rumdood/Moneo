using System;

namespace Moneo.Functions
{
    internal static class MoneoConfiguration
    {
        internal class QuietHoursSetting
        {
            public DateTime Start { get; init; }
            public DateTime End { get; init; }
            public string TimeZone { get; init; }
        }

        private const int DefaultOldDueDatesMaxDays = 7;
        private const int DefaultCompletionHistoryEventCount = 5;

        public static string DefaultCompletedMessage { get => Environment.GetEnvironmentVariable("defaultCompletedMessage"); }
        public static string DefaultSkippedMessage { get => Environment.GetEnvironmentVariable("defaulteSkippedMessage"); }
        public static string DefaultTaskDueMessage { get => Environment.GetEnvironmentVariable("defaultTaskDueMessage"); }
        public static string DefaultReminderMessage { get => Environment.GetEnvironmentVariable("defaultReminderMessage"); }
        public static int OldDueDatesMaxDays { get => GetIntFromEnvironment("oldDueDatesMaxDays", DefaultOldDueDatesMaxDays); }
        public static int MaxCompletionHistoryEventCount { get => GetIntFromEnvironment("completionHistoryEventCount", DefaultCompletionHistoryEventCount); }
        public static string TelegramBotToken { get 
                => Environment.GetEnvironmentVariable("telegramBotToken", EnvironmentVariableTarget.Process) 
                ?? throw new ArgumentException("Telegram Token Not Found"); }
        public static string ChatServiceEndpoint { get => Environment.GetEnvironmentVariable("chatServiceEndpoint"); }
        public static string ChatServiceApiKey { get => Environment.GetEnvironmentVariable("chatServiceApiKey"); }

        public static QuietHoursSetting QuietHours =>
            new()
            {
                Start = DateTime.Parse(Environment.GetEnvironmentVariable("quietHours__start") ?? string.Empty),
                End = DateTime.Parse(Environment.GetEnvironmentVariable("quietHours__end") ?? string.Empty),
                TimeZone = Environment.GetEnvironmentVariable("quietHours__timezone")
            };

        private static int GetIntFromEnvironment(string key, int defaultValue)
        {
            return int.TryParse(Environment.GetEnvironmentVariable(key), out var value) ? value : defaultValue;
        }

        private static long GetLongFromEnvironment(string key, int defaultValue)
        {
            if (long.TryParse(Environment.GetEnvironmentVariable(key), out var value))
            {
                return value;
            }

            return defaultValue;
        }

    }
}
