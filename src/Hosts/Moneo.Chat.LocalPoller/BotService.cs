using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moneo.Obsolete.TaskManagement;

namespace Moneo.Chat;

internal class BotService : BackgroundService
{
    private readonly IChatAdapter _chatAdapter;
    private readonly ITaskResourceManager _taskResourceManager;
    private readonly ILogger<BotService> _logger;
    private readonly IHostApplicationLifetime _appLifetime;

    public BotService(
        IChatAdapter chatAdapter,
        ITaskResourceManager taskResourceManager,
        ILogger<BotService> logger,
        IHostApplicationLifetime appLifetime)
    {
        _chatAdapter = chatAdapter;
        _taskResourceManager = taskResourceManager;
        _logger = logger;
        _appLifetime = appLifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _taskResourceManager.InitializeAsync();
        await _chatAdapter.StartReceivingAsync(stoppingToken);
        _logger.LogInformation("Moneo Bot Service Is Running");

        while (!stoppingToken.IsCancellationRequested && _chatAdapter.IsActive)
        {
            // loopaloopa
            await Task.Delay(1000, stoppingToken); // Add a delay to prevent a tight loop
        }
        
        _logger.LogInformation("Stopping Bot");
        _appLifetime.StopApplication();
    }
}
