using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StickerBot.Options;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace StickerBot.Services
{
    public class WebhookService : IHostedService
    {
        private readonly ILogger<WebhookService> _logger;
        private readonly ITelegramBotClient _bot;
        private readonly BotOptions _botConfig;

        public WebhookService(ILogger<WebhookService> logger, IConfiguration configuration, ITelegramBotClient bot)
        {
            _logger = logger;
            _botConfig = configuration.Get<BotOptions>();
            _bot = bot;
        }

        public async Task StartAsync(CancellationToken ct)
        {
            var webhookAddress = $"{_botConfig.WebhookUrl}/bot/{_botConfig.BotToken}";

            _logger.LogInformation("Setting webhook: ", webhookAddress);
            
            await _bot.SetWebhookAsync(
                url: webhookAddress,
                allowedUpdates: new[] { UpdateType.Message, UpdateType.CallbackQuery },
                cancellationToken: ct);
        }

        public Task StopAsync(CancellationToken ct)
        {
            _logger.LogInformation("Stopping webhook service...");

            return Task.CompletedTask;
        }
    }
}
