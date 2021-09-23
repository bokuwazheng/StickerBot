using JournalApiClient.Data;
using JournalApiClient.Data.Constants;
using JournalApiClient.Data.Enums;
using JournalApiClient.Extensions;
using JournalApiClient.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using StickerBot.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace StickerBot.Services
{
    public class SuggestionHandler
    {
        private readonly ITelegramBotClient _bot;
        private readonly ILogger<SuggestionHandler> _logger;
        private readonly IJournalApiClient _repo;
        private readonly BotOptions _options;

        public SuggestionHandler(ITelegramBotClient botClient, ILogger<SuggestionHandler> logger, IJournalApiClient repo, IOptions<BotOptions> options)
        {
            _bot = botClient;
            _logger = logger;
            _repo = repo;
            _options = options.Value;
        }

        public async Task HandleReviewAsync(CallbackQuery callbackQuery, CancellationToken ct)
        {
            _logger.LogInformation("Recieved a review");

            ReviewLite review = JsonConvert.DeserializeObject<ReviewLite>(callbackQuery.Data);

            if (review.Result is not ReviewResult.None)
            {
                await _repo.AddReviewAsync(new(review), ct).ConfigureAwait(false);

                Sender sender = await _repo.GetSuggesterAsync(review.SuggestionId, ct).ConfigureAwait(false);

                if (review.Result is ReviewResult.Banned)
                    await BanAsync(callbackQuery.Id, sender, ct).ConfigureAwait(false);
                else
                if (sender.Notify)
                {
                    await NotifyAsync(review, sender.UserId, ct).ConfigureAwait(false);
                }

                await _bot.EditMessageCaptionAsync(review.SuggesterId, callbackQuery.Message.MessageId, review.Result.ToDescription(), null, ct).ConfigureAwait(false);
            }

            Suggestion suggestion = await _repo.GetNewSuggestionAsync(ct).ConfigureAwait(false);

            if (suggestion is not null)
                await SendForReviewAsync(suggestion, ct).ConfigureAwait(false);
        }

        public async Task HandleNewSuggestionAsync(Message message, CancellationToken ct)
        {
            _logger.LogInformation("Recieved new suggestion");

            string ext = Path.GetExtension(message.Document.FileName).ToLower();
            if (ext is not ".jpg" or ".jpeg" or ".png")
                return;

            int userId = message.From.Id;
            string fileId = message.Document.FileId;

            Suggestion suggestion = await _repo.AddSuggestionAsync(fileId, userId, ct).ConfigureAwait(false);
            string text = string.Format(Reply.ThankYou, suggestion.Id);
            await _bot.SendTextMessageAsync(userId, text, cancellationToken: ct).ConfigureAwait(false);

            Suggestion unreviewed = await _repo.GetNewSuggestionAsync(ct).ConfigureAwait(false);

            if (suggestion.Id == unreviewed.Id)
                await SendForReviewAsync(suggestion, ct).ConfigureAwait(false);
        }

        private async Task SendForReviewAsync(Suggestion suggestion, CancellationToken ct)
        {
            _logger.LogInformation("Sending suggestion {id} for review", suggestion.Id);

            ReviewLite model = new(suggestion.Id, _options.ChatId);
            Dictionary<string, string> map = new();
            ReviewResult[] values = Enum.GetValues<ReviewResult>();

            foreach (ReviewResult result in values)
            {
                model.Result = result;
                string resultJson = JsonConvert.SerializeObject(model);
                map.Add(result.ToDescription(), resultJson);
            }

            InlineKeyboardMarkup markup = new(map
                .Select(item => new[] { InlineKeyboardButton.WithCallbackData(item.Key, item.Value) }).ToArray());

            Sender sender = await _repo.GetSuggesterAsync(suggestion.Id, ct).ConfigureAwait(false);
            await _bot.SendDocumentAsync(_options.ChatId, new(suggestion.FileId), $"From { sender.Username }", replyMarkup: markup, cancellationToken: ct);
        }

        /// <summary>
        /// Ban sender.
        /// </summary>
        private async Task BanAsync(string callbackQueryId, Sender sender, CancellationToken ct)
        {
            _logger.LogInformation("Banning user {userId}", sender.UserId);

            sender = sender with { IsBanned = true };
            await _repo.UpdateSenderAsync(sender, ct).ConfigureAwait(false);

            string text = $"User { sender.Username } got banned.";
            await _bot.AnswerCallbackQueryAsync(callbackQueryId, text, cancellationToken: ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Notify sender about status change.
        /// </summary>
        private async Task NotifyAsync(ReviewLite review, int userId, CancellationToken ct)
        {
            _logger.LogInformation("Notifying user {userId}", userId);

            Suggestion suggestion = await _repo.GetSuggestionAsync(review.SuggestionId, ct).ConfigureAwait(false);

            string reply = review.Result is ReviewResult.Approved
                ? Reply.Approved
                : string.Format(Reply.Declined, review.Result.ToDescription());

            await _bot.SendDocumentAsync(userId, new(suggestion.FileId), reply, cancellationToken: ct).ConfigureAwait(false);
        }
    }
}
