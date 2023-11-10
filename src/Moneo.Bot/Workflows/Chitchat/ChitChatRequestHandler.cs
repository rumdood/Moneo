using MediatR;
using Microsoft.Extensions.Logging;
using Moneo.Bot.Commands;

namespace Moneo.Bot.Workflows.Chitchat;

internal class ChitChatRequestHandler : IRequestHandler<ChitChatRequest, MoneoCommandResult>
{
    private readonly ILogger<ChitChatRequestHandler> _logger;

    public ChitChatRequestHandler(ILogger<ChitChatRequestHandler> logger)
    {
        _logger = logger;
    }
    
    public Task<MoneoCommandResult> Handle(ChitChatRequest request, CancellationToken cancellationToken)
    {
        _logger.LogWarning("Received idle chitchat or unknown command: {@Text}", request.UserText);
        return Task.FromResult(new MoneoCommandResult
        {
            ResponseType = ResponseType.Text,
            Type = ResultType.Error,
            UserMessageText = "I don't do idle chitchat...yet"
        });
    }
}