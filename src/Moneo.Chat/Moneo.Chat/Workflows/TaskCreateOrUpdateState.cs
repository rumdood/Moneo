namespace Moneo.Chat.Workflows;

public enum TaskCreateOrUpdateState
{
    Start,
    WaitingForName,
    WaitingForDescription,
    WaitingForTimezone,
    WaitingForCompletedMessage,
    WaitingForSkippedMessage,
    WaitingForRepeater,
    WaitingForRepeaterCron,
    WaitingForRepeaterExpiry,
    WaitingForRepeaterCompletionThreshold,
    WaitingForBadger,
    WaitingForBadgerFrequency,
    WaitingForBadgerMessages,
    WaitingForDueDates,
    WaitingForUserDirection,
    End
}
