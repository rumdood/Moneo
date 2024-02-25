using Microsoft.Extensions.DependencyInjection;
using Moneo.Chat;
using Moneo.Core;

namespace Moneo.Functions.Isolated;

internal static class ServiceProviderExtensions
{
    public static IServiceCollection AddBotConfiguration(this IServiceCollection serviceCollection)
    {
        IBotClientConfiguration botConfig = new BotClientConfiguration();
        botConfig.BotToken = Environment.GetEnvironmentVariable("telegramBotToken", EnvironmentVariableTarget.Process) 
                             ?? throw new ArgumentException("Telegram Token Not Found");
        botConfig.CallbackToken = Environment.GetEnvironmentVariable("callbackToken", EnvironmentVariableTarget.Process) 
                                  ?? throw new ArgumentException("Callback Token Not Found");
        botConfig.FunctionKey = Environment.GetEnvironmentVariable("taskManagerKey", EnvironmentVariableTarget.Process)
                                ?? throw new ArgumentException("Task Manager Token Not Found");
        botConfig.MasterConversationId = Convert.ToInt64(Environment.GetEnvironmentVariable("telegramChatId", EnvironmentVariableTarget.Process) 
                                                         ?? throw new ArgumentException("Telegram Chat Id Not Found"));
        botConfig.TaskApiBase = Environment.GetEnvironmentVariable("taskManagerApiBase", EnvironmentVariableTarget.Process) 
                                ?? throw new ArgumentException("Telegram Token Not Found");

        serviceCollection.AddScoped(_ => botConfig);

        return serviceCollection;
    }
}