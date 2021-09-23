using JournalApiClient.Data;
using JournalApiClient.Data.Constants;
using JournalApiClient.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using StickerBot.Services;

namespace StickerBot.Controllers
{
    public class WebhookController : ControllerBase
    {
        private readonly ILogger<WebhookController> _logger;
        private readonly ITelegramBotClient _bot;
        private readonly IJournalApiClient _repo;

        public WebhookController(ILogger<WebhookController> logger, ITelegramBotClient bot, IJournalApiClient repo)
        {
            _logger = logger;
            _bot = bot;
            _repo = repo;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Update update, [FromServices] CommandHandler commandHander, [FromServices] SuggestionHandler suggestionHandler)
        {
            if (update is null)
                return Ok();

            _logger.LogInformation("Received an update of type {type}", update.Type);

            User user = update.Message.From;

            Sender sender = await _repo.GetSenderAsync(user.Id).ConfigureAwait(false);

            if (sender is { IsBanned: true })
            {
                await _bot.SendTextMessageAsync(user.Id, Reply.Banned).ConfigureAwait(false);
                return Ok();
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
                await _repo.AddSenderAsync(sender, CancellationToken.None).ConfigureAwait(false);
            }

            Task task = update.Type switch
            {
                UpdateType.CallbackQuery => suggestionHandler.HandleReviewAsync(update.CallbackQuery, CancellationToken.None),
                UpdateType.Message when update.Message.Type is MessageType.Document => suggestionHandler.HandleNewSuggestionAsync(update.Message, CancellationToken.None),
                UpdateType.Message when update.Message.Type is MessageType.Text => commandHander.HandleAsync(update.Message, CancellationToken.None),
                UpdateType.Message => HandleUnsupportedAsync(update.Message, CancellationToken.None),
                _ => null
            };

            if (task is not null)
                await task.ConfigureAwait(false);

            return Ok();
        }

        /// <summary>
        /// Handle unsupported message type.
        /// </summary>
        private async Task HandleUnsupportedAsync(Message message, CancellationToken ct)
        {
            _logger.LogInformation("Received a message of unsupported type {type} from {id}", message.Type, message.From.Id);
            await _bot.SendTextMessageAsync(message.Chat.Id, Reply.WrongUpdateType, cancellationToken: ct);
        }
    }
}
