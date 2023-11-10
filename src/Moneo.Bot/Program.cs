using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moneo.Bot;
using Moneo.Bot.Commands;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblies(typeof(IMoneoCommand).Assembly);
});

builder.Services.AddSingleton<IConversationManager, ConversationManager>();
builder.Services.Configure<BotClientConfiguration>(builder.Configuration.GetSection(nameof(BotClientConfiguration)));
builder.Services.AddHostedService<BotService>();

var host = builder.Build();
await host.RunAsync();