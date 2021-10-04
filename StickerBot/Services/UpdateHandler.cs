using JournalApiClient.Data.Constants;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace StickerBot.Services
{
    public class UpdateHandler
    {
        private readonly ILogger<WebhookService> _logger;
        private readonly ITelegramBotClient _bot;
        private readonly SenderHandler _senderHandler;
        private readonly CommandHandler _commandHander;
        private readonly SubmissionHandler _suggestionHandler;

        public UpdateHandler(
            ILogger<WebhookService> logger,
            ITelegramBotClient bot,
            SenderHandler senderHandler,
            CommandHandler commandHander,
            SubmissionHandler suggestionHandler)
        {
            _logger = logger;
            _bot = bot;
            _senderHandler = senderHandler;
            _commandHander = commandHander;
            _suggestionHandler = suggestionHandler;
        }

        public async Task HandleAsync(Update update, CancellationToken ct = default)
        {
            if (update is null)
                return;

            _logger.LogInformation("Handling update of type {type}", update.Type);

            bool isWelcome = await _senderHandler.IsWelcomeAsync(update, ct);
            if (!isWelcome)
                return;

            Task task = update.Type switch
            {
                UpdateType.CallbackQuery => _suggestionHandler.HandleReviewAsync(update.CallbackQuery, ct),
                UpdateType.Message when update.Message.Type is MessageType.Document => _suggestionHandler.HandleNewSuggestionAsync(update.Message, ct),
                UpdateType.Message when update.Message.Type is MessageType.Text => _commandHander.HandleAsync(update.Message, ct),
                UpdateType.Message => HandleUnsupportedAsync(update.Message, ct),
                _ => null
            };

            if (task is not null)
                await task;
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
