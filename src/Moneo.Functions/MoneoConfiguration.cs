using System;

namespace Moneo.Functions
{
    internal static class MoneoConfiguration
    {
        private const int DefaultDefuseThresholdHours = 4;
        private const int DefaultOldDueDatesMaxDays = 7;

        public static string DefaultCompletedMessage { get => Environment.GetEnvironmentVariable("defaultCompletedMessage"); }
        public static string DefaultSkippedMessage { get => Environment.GetEnvironmentVariable("defaulteSkippedMessage"); }
        public static string DefaultTaskDueMessage { get => Environment.GetEnvironmentVariable("defaultTaskDueMessage"); }
        public static string DefaultReminderMessage { get => Environment.GetEnvironmentVariable("defaultReminderMessage"); }
        public static int DefuseThresholdHours { get => GetIntFromEnvironment("defuseThresholdHours", DefaultDefuseThresholdHours); }
        public static int OldDueDatesMaxDays { get => GetIntFromEnvironment("oldDueDatesMaxDays", DefaultOldDueDatesMaxDays); }

        private static int GetIntFromEnvironment(string key, int defaultValue)
        {
            if (int.TryParse(Environment.GetEnvironmentVariable(key), out var value))
            {
                return value;
            }

            return defaultValue;
        }
    }
}
