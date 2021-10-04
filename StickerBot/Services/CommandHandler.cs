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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace StickerBot.Services
{
    public class CommandHandler
    {
        private readonly ITelegramBotClient _bot;
        private readonly ILogger<CommandHandler> _logger;
        private readonly IJournalApiClient _repo;
        private readonly BotOptions _options;

        public CommandHandler(ITelegramBotClient botClient, ILogger<CommandHandler> logger, IJournalApiClient repo, IOptions<BotOptions> options)
        {
            _bot = botClient;
            _logger = logger;
            _repo = repo;
            _options = options.Value;
        }

        public async Task HandleAsync(Message message, CancellationToken ct = default)
        {
            int userId = message.From.Id;

            string[] words = message.Text.Split(' ');
            string command = words[0];
            string argument = words.Length > 1 ? words[1] : null;

            Task task = command switch
            {
                "/start" => HandleStartCommadAsync(userId, ct),
                "/subscribe" => HandleSubscribeCommadAsync(true, userId, ct),
                "/unsubscribe" => HandleSubscribeCommadAsync(false, userId, ct),
                "/guidelines" => HandleGuidelinesCommadAsync(userId, ct),
                "/status" when int.TryParse(argument, out int suggestionId) => HandleStatusCommadAsync(suggestionId, userId, ct),
                "/status" => HandleStatusCommadAsync(userId, ct),
                "/review" => HandleReviewCommandAsync(userId, ct),
                _ => HandleUnknownCommadAsync(userId, ct)
            };

            await task;
        }

        private async Task HandleReviewCommandAsync(int userId, CancellationToken ct)
        {
            if (userId != _options.ChatId)
                return;

            Suggestion suggestion = await _repo.GetNewSuggestionAsync(ct);

            if (suggestion is null)
                return;

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

            Sender sender = await _repo.GetSuggesterAsync(suggestion.Id, ct);
            await _bot.SendDocumentAsync(_options.ChatId, new(suggestion.FileId), $"From { sender.Username }", replyMarkup: markup, cancellationToken: ct);
        }

        /// <summary>
        /// Greet and inform about the guidelines.
        /// </summary>
        private async Task HandleStartCommadAsync(int userId, CancellationToken ct)
        {
            _logger.LogInformation("Greeting user {userId}", userId);

            await _bot.SendTextMessageAsync(userId, Reply.Hello, cancellationToken: ct);
            await _bot.SendTextMessageAsync(userId, string.Format(Reply.BeforeSubmitting, _options.Guidelines), cancellationToken: ct);
        }

        /// <summary>
        /// Send status of a submission.
        /// </summary>
        private async Task HandleStatusCommadAsync(int suggestionId, int userId, CancellationToken ct)
        {
            _logger.LogInformation("Sending submission {suggestionId} status to user {userId}", suggestionId, userId);

            Suggestion suggestion = await _repo.GetSuggestionAsync(suggestionId, ct);

            if (suggestion is null)
            {
                await _bot.SendTextMessageAsync(userId, string.Format(Reply.SuggestionNotFound, suggestionId), cancellationToken: ct);
                return;
            }

            if (suggestion.UserId != userId)
            {
                await _bot.SendTextMessageAsync(userId, Reply.StatusUnavaliable, cancellationToken: ct);
                return;
            }

            Review review = await _repo.GetReviewAsync(suggestionId, ct);

            string reply = review.ResultCode is ReviewResult.Approved
                ? Reply.Approved
                : string.Format(Reply.Declined, review.ResultCode.ToDescription());

            await _bot.SendTextMessageAsync(userId, reply, cancellationToken: ct);
        }

        /// <summary>
        /// Send status of the latest submission.
        /// </summary>
        private async Task HandleStatusCommadAsync(int userId, CancellationToken ct)
        {
            _logger.LogInformation("Sending latest submission status to {userId}", userId);

            Review review = await _repo.GetNewReviewAsync(userId, ct);

            if (review is null)
            {
                await _bot.SendTextMessageAsync(userId, Reply.LatestNotYetReviewed, cancellationToken: ct);
                return;
            }

            string reply = review.ResultCode is ReviewResult.Approved
                ? Reply.Approved
                : string.Format(Reply.Declined, review.ResultCode.ToDescription());

            await _bot.SendTextMessageAsync(userId, reply, cancellationToken: ct);
        }

        /// <summary>
        /// Change subscription status and inform about the result.
        /// </summary>
        private async Task HandleSubscribeCommadAsync(bool notify, int userId, CancellationToken ct)
        {
            _logger.LogInformation("Setting user {userId} subscriptions status to {notify}", userId, notify);

            Sender sender = await _repo.GetSenderAsync(userId, ct);
            string reply;

            if (sender.Notify == notify)
            {
                reply = notify ? Reply.AlreadySubscribed : Reply.AlreadyUnsubscribed;
                await _bot.SendTextMessageAsync(userId, reply, cancellationToken: ct);
                return;
            }

            sender = sender with { Notify = notify };
            sender = await _repo.UpdateSenderAsync(sender, ct);
            reply = sender.Notify ? Reply.Subscribed : Reply.Unsubscribed;
            await _bot.SendTextMessageAsync(userId, reply, cancellationToken: ct);
        }

        /// <summary>
        /// Send the guidelines.
        /// </summary>
        private async Task HandleGuidelinesCommadAsync(int userId, CancellationToken ct)
        {
            _logger.LogInformation("Sending guidelines to user {userId}", userId);

            await _bot.SendTextMessageAsync(userId, string.Format(Reply.Guidelines, _options.Guidelines), cancellationToken: ct);
        }

        /// <summary>
        /// Inform about avaliable uses.
        /// </summary>
        private async Task HandleUnknownCommadAsync(int userId, CancellationToken ct)
        {
            _logger.LogInformation("Informing user {userId} about avaliable uses", userId);

            await _bot.SendTextMessageAsync(userId, Reply.WrongCommand, cancellationToken: ct);
        }
    }
}
