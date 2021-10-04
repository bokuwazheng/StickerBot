using JournalApiClient.Data.Constants;
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

        public WebhookController(ILogger<WebhookController> logger, ITelegramBotClient bot)
        {
            _logger = logger;
            _bot = bot;
        }

        [HttpPost]
        public async Task<IActionResult> Post(
            [FromBody] Update update, 
            [FromServices] SenderHandler senderHandler,
            [FromServices] CommandHandler commandHander, 
            [FromServices] SubmissionHandler suggestionHandler)
        {
            if (update is null)
                return BadRequest();

            _logger.LogInformation("Received an update of type {type}", update.Type);

            bool isWelcome = await senderHandler.IsWelcomeAsync(update);
            if (!isWelcome)
                return Ok();

            Task task = update.Type switch
            {
                UpdateType.CallbackQuery => suggestionHandler.HandleReviewAsync(update.CallbackQuery),
                UpdateType.Message when update.Message.Type is MessageType.Document => suggestionHandler.HandleNewSuggestionAsync(update.Message),
                UpdateType.Message when update.Message.Type is MessageType.Text => commandHander.HandleAsync(update.Message),
                UpdateType.Message => HandleUnsupportedAsync(update.Message),
                _ => null
            };

            if (task is not null)
                await task;

            return Ok();
        }

        /// <summary>
        /// Handle unsupported message type.
        /// </summary>
        private async Task HandleUnsupportedAsync(Message message, CancellationToken ct = default)
        {
            _logger.LogInformation("Received a message of unsupported type {type} from {id}", message.Type, message.From.Id);

            await _bot.SendTextMessageAsync(message.Chat.Id, Reply.WrongUpdateType, cancellationToken: ct);
        }
    }
}
