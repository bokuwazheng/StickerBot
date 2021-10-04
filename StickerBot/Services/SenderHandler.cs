using JournalApiClient.Data;
using JournalApiClient.Data.Constants;
using JournalApiClient.Services;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace StickerBot.Services
{
    public class SenderHandler
    {
        private readonly ITelegramBotClient _bot;
        private readonly ILogger<CommandHandler> _logger;
        private readonly IJournalApiClient _repo;

        public SenderHandler(ITelegramBotClient botClient, ILogger<CommandHandler> logger, IJournalApiClient repo)
        {
            _bot = botClient;
            _logger = logger;
            _repo = repo;
        }

        public async Task<bool> IsWelcomeAsync(Update update, CancellationToken ct = default)
        {
            _logger.LogInformation("Checking user...");

            User user = update.Type switch
            {
                UpdateType.Message => update.Message.From,
                UpdateType.CallbackQuery => update.CallbackQuery.From,
                _ => null
            };

            Sender sender = await _repo.GetSenderAsync(user.Id);

            if (sender is { IsBanned: true })
            {
                await _bot.SendTextMessageAsync(user.Id, Reply.Banned, cancellationToken: ct);
                return false;
            }

            if (sender is null)
            {
                sender = new()
                {
                    UserId = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Username = user.Username,
                    ChatId = update.Message.Chat.Id
                };

                _logger.LogInformation("Adding new sender {id}", user.Id);

                await _repo.AddSenderAsync(sender, ct);
            }

            return true;
        }
    }
}
