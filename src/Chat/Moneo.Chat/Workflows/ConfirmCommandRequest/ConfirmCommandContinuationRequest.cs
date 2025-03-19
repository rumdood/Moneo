using MediatR;
using Moneo.Chat.Commands;

namespace Moneo.Chat
{
    [UserCommand(CommandKey = "/continueConfirmCommand")]
    public partial class ConfirmCommandContinuationRequest : UserRequestBase
    {
        public string Text { get; }

        public ConfirmCommandContinuationRequest(long conversationId, params string[] args) : base(conversationId, args)
        {
            Text = string.Join(' ', args);
        }

        public ConfirmCommandContinuationRequest(long conversationId, string text) : base(conversationId, text)
        {
            Text = text;
        }
    }

    internal class ConfirmCommandContinuationRequestHandler : IRequestHandler<ConfirmCommandContinuationRequest, MoneoCommandResult>
    {
        private readonly IConfirmCommandWorkflowManager _manager;

        public ConfirmCommandContinuationRequestHandler(IConfirmCommandWorkflowManager manager)
        {
            _manager = manager;
        }

        public Task<MoneoCommandResult> Handle(ConfirmCommandContinuationRequest request, CancellationToken cancellationToken)
            => _manager.ContinueWorkflowAsync(request.ConversationId, request.Text);
    }
}
