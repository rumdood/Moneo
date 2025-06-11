using Moneo.Chat;

namespace Moneo.Hosts.Chat.Api;

public class TaskChatStates : ChatStateProviderBase
{
    public static readonly ChatState CreateTask = ChatState.Register(nameof(CreateTask));
    public static readonly ChatState ChangeTask = ChatState.Register(nameof(ChangeTask));
    public static readonly ChatState CompleteTask = ChatState.Register(nameof(CompleteTask));
    public static readonly ChatState SkipTask = ChatState.Register(nameof(SkipTask));
}