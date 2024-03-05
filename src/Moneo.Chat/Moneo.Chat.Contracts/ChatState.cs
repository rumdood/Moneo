namespace Moneo.Chat;

public enum ChatState
{
    Waiting,
    CreateTask,
    CreateCron,
    CompleteTask,
    SkipTask,
    ConfirmCommand,
}