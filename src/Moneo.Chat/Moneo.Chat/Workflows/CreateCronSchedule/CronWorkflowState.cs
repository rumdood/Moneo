namespace Moneo.Chat.Workflows.CreateCronSchedule;

internal enum CronWorkflowState
{
    Start,
    WaitingForDailyOrSpecific,
    WaitingForWeekOrMonthDays,
    WaitingForDaysOfWeek,
    WaitingForDaysOfMonth,
    WaitingForTimesOfDay,
    Complete
}