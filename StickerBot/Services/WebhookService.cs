using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StickerBot.Options;
using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace StickerBot.Services
{
    public class WebhookService : IHostedService
    {
        private readonly ILogger<WebhookService> _logger;
        private readonly IServiceProvider _services;
        private readonly BotOptions _botConfig;

        public WebhookService(ILogger<WebhookService> logger, IServiceProvider serviceProvider, IConfiguration configuration)
        {
            _logger = logger;
            _services = serviceProvider;
            _botConfig = configuration.Get<BotOptions>();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _services.CreateScope();
            var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();

            //var webhookAddress = $"{_botConfig.WebhookUrl}/bot/{_botConfig.BotToken}";
            var webhookAddress = _botConfig.WebhookUrl;

            _logger.LogInformation("Setting webhook: ", webhookAddress);
            
            await botClient.SetWebhookAsync(
                url: webhookAddress,
                allowedUpdates: Array.Empty<UpdateType>(),
                cancellationToken: cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            using var scope = _services.CreateScope();
            var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();

            _logger.LogInformation("Removing webhook");

            await botClient.DeleteWebhookAsync(cancellationToken: cancellationToken);
        }
    }
}
