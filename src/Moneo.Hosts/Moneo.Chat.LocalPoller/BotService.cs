using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moneo.TaskManagement;

namespace Moneo.Chat;

internal class BotService : BackgroundService
{
    private readonly IChatAdapter _chatAdapter;
    private readonly ITaskResourceManager _taskResourceManager;
    private readonly ILogger<BotService> _logger;

    public BotService(IChatAdapter chatAdapter, ITaskResourceManager taskResourceManager, ILogger<BotService> logger)
    {
        _chatAdapter = chatAdapter;
        _taskResourceManager = taskResourceManager;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _taskResourceManager.InitializeAsync();
        await _chatAdapter.StopReceivingAsync(stoppingToken);
        _chatAdapter.StartReceiving(stoppingToken);
        _logger.LogInformation("Moneo Bot Service Is Running");

        while (!stoppingToken.IsCancellationRequested)
        {
            // loopaloopa
        }
        
        _logger.LogInformation("Stopping Bot");
    }
}
