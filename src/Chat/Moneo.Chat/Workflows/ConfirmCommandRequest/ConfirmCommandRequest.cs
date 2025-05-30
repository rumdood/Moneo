using Moneo.Chat.Models;

namespace Moneo.Chat;

[UserCommand(CommandKey = "/confirmCommand")]
public partial class ConfirmCommandRequest : UserRequestBase
{
    public string PotentialCommand { get; private set; }
    public string PotentialArguments { get; private set; }

    public ConfirmCommandRequest(CommandContext context) : base(context)
    {
        if (context.Args.Length == 0)
        {
            throw new ArgumentException("No potential command provided");
        }

        PotentialCommand = context.Args[0].StartsWith('/') ? context.Args[0] : $"/{context.Args[0]}";
        PotentialArguments = context.Args.Length > 1
            ? string.Join(' ', context.Args.Skip(1))
            : "";
    }

    public ConfirmCommandRequest(long conversationId, ChatUser? user, string potentialCommand, string[] potentialArguments) : base(conversationId, user, potentialCommand)
    {
        PotentialCommand = potentialCommand.StartsWith('/') ? potentialCommand : $"/{potentialCommand}";
        PotentialArguments = string.Join(' ', potentialArguments);
    }
}