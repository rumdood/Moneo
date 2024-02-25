﻿using System;

namespace Moneo.Functions.Isolated
{
    internal static class MoneoConfiguration
    {
        internal class QuietHoursSetting
        {
            public DateTime Start { get; init; }
            public DateTime End { get; init; }
            public string TimeZone { get; init; }
        }

        private const int DefaultDefuseThresholdHours = 4;
        private const int DefaultOldDueDatesMaxDays = 7;
        private const int DefaultCompletionHistoryEventCount = 5;

        public static string DefaultCompletedMessage { get => Environment.GetEnvironmentVariable("defaultCompletedMessage"); }
        public static string DefaultSkippedMessage { get => Environment.GetEnvironmentVariable("defaulteSkippedMessage"); }
        public static string DefaultTaskDueMessage { get => Environment.GetEnvironmentVariable("defaultTaskDueMessage"); }
        public static string DefaultReminderMessage { get => Environment.GetEnvironmentVariable("defaultReminderMessage"); }
        public static int DefuseThresholdHours { get => GetIntFromEnvironment("defuseThresholdHours", DefaultDefuseThresholdHours); }
        public static int OldDueDatesMaxDays { get => GetIntFromEnvironment("oldDueDatesMaxDays", DefaultOldDueDatesMaxDays); }
        public static int MaxCompletionHistoryEventCount { get => GetIntFromEnvironment("completionHistoryEventCount", DefaultCompletionHistoryEventCount); }
        public static string TelegramBotToken { get 
                => Environment.GetEnvironmentVariable("telegramBotToken", EnvironmentVariableTarget.Process) 
                ?? throw new ArgumentException("Telegram Token Not Found"); }
        public static string BotUri { get => Environment.GetEnvironmentVariable("botUri"); }
        public static string BotClientId { get => Environment.GetEnvironmentVariable("botClientId"); }
        public static long LegacyChatId { get => GetLongFromEnvironment("telegramChatId", -1); }

        public static QuietHoursSetting QuietHours
        {
            get
            {
                return new QuietHoursSetting
                {
                    Start = DateTime.Parse(Environment.GetEnvironmentVariable("quietHours__start")),
                    End = DateTime.Parse(Environment.GetEnvironmentVariable("quietHours__end")),
                    TimeZone = Environment.GetEnvironmentVariable("quietHours__timezone")
                };
            }
        }

        private static int GetIntFromEnvironment(string key, int defaultValue)
        {
            if (int.TryParse(Environment.GetEnvironmentVariable(key), out var value))
            {
                return value;
            }

            return defaultValue;
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
