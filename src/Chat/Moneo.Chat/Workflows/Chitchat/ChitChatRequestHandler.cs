using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moneo.Chat.Commands;

namespace Moneo.Chat.Workflows.Chitchat;

internal class ChitChatRequestHandler : IRequestHandler<ChitChatRequest, MoneoCommandResult>
{
    private readonly IChitChatWorkflowManager? _chitChatWorkflowManager;
    private readonly ILogger<ChitChatRequestHandler> _logger;
    
    private static MoneoCommandResult DefaultResult => new()
    {
        ResponseType = ResponseType.Text,
        Type = ResultType.Error,
        UserMessageText = "I don't do idle chitchat...yet"
    };

    public ChitChatRequestHandler(ILogger<ChitChatRequestHandler> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _chitChatWorkflowManager = serviceProvider.GetService<IChitChatWorkflowManager>();
    }
    
    public Task<MoneoCommandResult> Handle(ChitChatRequest request, CancellationToken cancellationToken)
    {
        _logger.LogWarning("Received idle chitchat or unknown command: {@Text}", request.UserText);
        if (_chitChatWorkflowManager is not null)
        {
            return _chitChatWorkflowManager.StartWorkflowAsync(request.ConversationId, request.ForUserId, request.UserText,
                cancellationToken);
        }

        _logger.LogWarning("No chitchat workflow found for idle chitchat");
        return Task.FromResult(DefaultResult);

    }
}
