namespace Moneo.Chat.Workflows.CreateTask;

internal enum TaskCreationState
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
    End
}
