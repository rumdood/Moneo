namespace Moneo.Chat.Workflows.CreateTask;

public class CreateTaskResponse
{
    public const string AskForNameResponse = "What do you want to call this task?";
    public const string AskForDescriptionResponse = "Provide a description for the task.";
    public const string EndOfWorkflowResponse = "I've created the task!";
    public const string SkippableResponse = "Should this task be skippable?";
    public const string SkippedMessageResponse = "What should I say when you skip the task?";
    public const string RepeaterResponse = "Does this task repeat?";
    public const string RepeaterExpiryResponse = "When should this task stop repeating?";
    public const string RepeaterCompletionThreshold = "How early can you complete the task and count it as completed?";
    public const string BadgerResponse = "Should I badger you about this?";
    public const string BadgerFrequencyResponse = "How often (in minutes)?";
    public const string DueDatesResponse = "When should this task be completed by?";
}