namespace Moneo.Chat.Workflows.CreateCronSchedule;

internal class CronStateMachine : IWorkflowStateMachine<CronWorkflowState>
{
    public CronWorkflowState CurrentState { get; private set; } = CronWorkflowState.Start;
    public CronDraft Draft { get; } = new();

    public CronWorkflowState GoToNext()
    {
        switch (CurrentState)
        {
            case CronWorkflowState.Start:
                CurrentState = CronWorkflowState.WaitingForDailyOrSpecific;
                break;
            case CronWorkflowState.WaitingForDailyOrSpecific:
                CurrentState = Draft.DayRepeatMode == DayRepeatMode.Daily
                    ? CronWorkflowState.WaitingForTimesOfDay
                    : CronWorkflowState.WaitingForWeekOrMonthDays;
                break;
            case CronWorkflowState.WaitingForWeekOrMonthDays:
                CurrentState = Draft.DayRepeatMode == DayRepeatMode.DayOfWeek
                    ? CronWorkflowState.WaitingForDaysOfWeek
                    : CronWorkflowState.WaitingForDaysOfMonth;
                break;
            case CronWorkflowState.WaitingForDaysOfWeek:
            case CronWorkflowState.WaitingForDaysOfMonth:
                if (Draft.IsDaysToRepeatComplete)
                { 
                    CurrentState = CronWorkflowState.WaitingForTimesOfDay;
                }
                break;
            case CronWorkflowState.WaitingForTimesOfDay:
                if (Draft.IsTimesToRepeatComplete)
                {
                    CurrentState = CronWorkflowState.Complete;
                }
                break;
            case CronWorkflowState.Complete:
            default:
                break;
        }

        return CurrentState;
    }
}