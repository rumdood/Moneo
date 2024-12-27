namespace Moneo.Chat;

public enum ChatState
{
    Waiting,
    CreateTask,
    ChangeTask,
    CreateCron,
    CompleteTask,
    SkipTask,
    ConfirmCommand,
}