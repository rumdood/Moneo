using Moneo.Chat.Models;

namespace Moneo.Chat;

[UserCommand(CommandKey = "/confirmCommand")]
public partial class ConfirmCommandRequest : UserRequestBase
{
    public string PotentialCommand { get; private set; }
    public string PotentialArguments { get; private set; }

    public ConfirmCommandRequest(long conversationId, ChatUser? user, params string[] args) : base(conversationId, user, args)
    {
        if (args.Length == 0)
        {
            throw new ArgumentException("No potential command provided");
        }

        PotentialCommand = args[0].StartsWith('/') ? args[0] : $"/{args[0]}";
        PotentialArguments = args.Length > 1
            ? string.Join(' ', args.Skip(1))
            : "";
    }

    public ConfirmCommandRequest(long conversationId, ChatUser? user, string potentialCommand, string[] potentialArguments) : base(conversationId, user, potentialCommand)
    {
        PotentialCommand = potentialCommand.StartsWith('/') ? potentialCommand : $"/{potentialCommand}";
        PotentialArguments = string.Join(' ', potentialArguments);
    }
}