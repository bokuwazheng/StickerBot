using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Microsoft.Extensions.Hosting;
using System;
using Telegram.Bot.Extensions.Polling;

namespace StickerBot.Services
{
    public class PollingService : BackgroundService
    {
        private readonly ILogger<PollingService> _logger;
        private readonly ITelegramBotClient _bot;
        private readonly UpdateHandler _updateHandler;

        public PollingService(ILogger<PollingService> logger, ITelegramBotClient bot, UpdateHandler updateHandler)
        {
            _logger = logger;
            _bot = bot;
            _updateHandler = updateHandler;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting polling service...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    DefaultUpdateHandler handler = new(
                        (_, u, ct) => _updateHandler.HandleAsync(u, ct),
                        (_, ex, _) => Task.FromException(ex),
                        new[] { UpdateType.Message, UpdateType.CallbackQuery });

                    await _bot.ReceiveAsync(handler, stoppingToken);
                }
                catch (TaskCanceledException) { }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occured while processing update");
                    await Task.Delay(TimeSpan.FromMilliseconds(500d), stoppingToken);
                }
            }

            _logger.LogInformation("Stopping polling service...");
        }
    }
}
