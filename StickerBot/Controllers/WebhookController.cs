using JournalApiClient.Data;
using JournalApiClient.Data.Constants;
using JournalApiClient.Data.Enums;
using JournalApiClient.Services;
using JournalApiClient.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Microsoft.Extensions.Options;
using StickerBot.Options;

namespace StickerBot.Controllers
{
    public class WebhookController : ControllerBase
    {
        private readonly ILogger<WebhookController> _logger;
        private readonly ITelegramBotClient _bot;
        private readonly IJournalApiClient _repo;
        private readonly BotOptions _options;

        public WebhookController(ILogger<WebhookController> logger, ITelegramBotClient bot, IJournalApiClient repo, IOptions<BotOptions> options)
        {
            _logger = logger;
            _bot = bot;
            _repo = repo;
            _options = options.Value;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Update update)
        {
            if (update is null)
                return Ok();

            _logger.LogInformation($"Received an update of type { update.Type }");

            User user = update.Type switch
            {
                UpdateType.Message => update.Message.From,
                UpdateType.CallbackQuery => update.CallbackQuery.From,
                _ => null
            };

            if (user is null || user.IsBot)
                return Ok();

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

                await _repo.AddSenderAsync(sender, CancellationToken.None).ConfigureAwait(false);
            }

            Task task = update.Type switch // TODO: consider making all this more readable and flexible
            {
                UpdateType.Message => HandleMessageAsync(update.Message, CancellationToken.None),
                UpdateType.CallbackQuery => HandleCallbackQueryAsync(update.CallbackQuery, CancellationToken.None),
                _ => null
            };

            if (task is not null)
                await task.ConfigureAwait(false);

            return Ok();
        }

        private async Task HandleMessageAsync(Message message, CancellationToken ct)
        {
            _logger.LogInformation($"Received a message of type { message.Type } from { message.From.Id }");

            Task task = message.Type switch
            {
                MessageType.Document => HandleDocumentMessageAsync(message, ct),
                MessageType.Text => HandleTextMessageAsync(message, ct),
                _ => _bot.SendTextMessageAsync(message.Chat.Id, Reply.WrongUpdateType, cancellationToken: ct)
            };

            await task.ConfigureAwait(false);
        }

        private async Task HandleDocumentMessageAsync(Message message, CancellationToken ct)
        {
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

        private async Task HandleTextMessageAsync(Message message, CancellationToken ct)
        {
            string command = null;
            string argument = null;

            bool match = Regex.IsMatch(message.Text, @"^\/[a-z]"); // TODO: consider using .StartsWith

            if (match)
            {
                string[] words = message.Text.Split(' ');
                command = words[0];
                argument = words.Length > 1 ? words[1] : null;
            }

            Task<string> task = command switch
            {
                "/start" => GreetAsync(message.From.Id, ct),
                "/status" => GetStatusAsync(argument, message.From.Id, ct),
                "/subscribe" => ToggleSubscriptionAsync(true, message.From.Id, ct),
                "/unsubscribe" => ToggleSubscriptionAsync(false, message.From.Id, ct),
                "/guidelines" => Task.FromResult(string.Format(Reply.Guidelines, _options.Guidelines)),
                _ => Task.FromResult(Reply.WrongUpdateType)
            };

            string response = await task.ConfigureAwait(false);
            await _bot.SendTextMessageAsync(message.Chat.Id, response, cancellationToken: ct).ConfigureAwait(false);
        }

        private async Task<string> GreetAsync(int userId, CancellationToken ct)
        {
            await _bot.SendTextMessageAsync(userId, Reply.Hello, cancellationToken: ct).ConfigureAwait(false);
            return string.Format(Reply.BeforeSubmitting, _options.Guidelines);
        }

        private async Task<string> GetStatusAsync(string id, int userId, CancellationToken ct)
        {
            Review review;
            bool byId = false;

            if (id is null)
                review = await _repo.GetNewReviewAsync(userId, ct).ConfigureAwait(false);
            else
            if (byId = int.TryParse(id, out int suggestionId))
            {
                Suggestion suggestion = await _repo.GetSuggestionAsync(suggestionId, ct).ConfigureAwait(false);

                if (suggestion is null)
                    return string.Format(Reply.SuggestionNotFound, suggestionId);

                if (suggestion.UserId != userId)
                    return Reply.StatusUnavaliable;

                review = await _repo.GetReviewAsync(suggestionId, ct).ConfigureAwait(false);
            }
            else
                return string.Format(Reply.InvalidId, id);


            if (review is null)
                return byId
                    ? Reply.NotYetReviewed
                    : Reply.LatestNotYetReviewed;

            string reply = review.ResultCode switch
            {
                ReviewResult.Approved => Reply.Approved,
                _ => string.Format(Reply.Declined, review.ResultCode.ToDescription())
            };

            return byId
                ? reply
                : string.Format(Reply.UseStatusN, reply);
        }

        private async Task<string> ToggleSubscriptionAsync(bool notify, int userId, CancellationToken ct)
        {
            Sender sender = await _repo.GetSenderAsync(userId, ct).ConfigureAwait(false);

            if (sender.Notify == notify)
                return notify ? Reply.AlreadySubscribed : Reply.AlreadyUnsubscribed;

            sender = sender with { Notify = notify };

            sender = await _repo.UpdateSenderAsync(sender, ct).ConfigureAwait(false);
            return sender.Notify ? Reply.Subscribed : Reply.Unsubscribed;
        }

        private async Task HandleCallbackQueryAsync(CallbackQuery callbackQuery, CancellationToken ct)
        {
            ReviewLite review = JsonConvert.DeserializeObject<ReviewLite>(callbackQuery.Data);
            if (review.Result is not ReviewResult.None)
            {
                await _repo.AddReviewAsync(new(review), ct).ConfigureAwait(false);

                Sender sender = await _repo.GetSuggesterAsync(review.SuggestionId, ct).ConfigureAwait(false);

                Task task = review.Result switch
                {
                    ReviewResult.Banned => BanAsync(callbackQuery.Id, sender, ct),
                    _ when sender.Notify => NotifyAsync(review, sender.UserId, ct),
                    _ => null
                };

                if (task is not null)
                    await task.ConfigureAwait(false);

                await _bot.EditMessageCaptionAsync(review.SuggesterId, callbackQuery.Message.MessageId, review.Result.ToDescription(), null, ct).ConfigureAwait(false);
            }

            await GetNextAsync(ct).ConfigureAwait(false);
        }

        private async Task GetNextAsync(CancellationToken ct)
        {
            Suggestion suggestion = await _repo.GetNewSuggestionAsync(ct).ConfigureAwait(false);

            if (suggestion is not null)
                await SendForReviewAsync(suggestion, ct).ConfigureAwait(false);
        }

        private async Task BanAsync(string callbackQueryId, Sender sender, CancellationToken ct)
        {
            sender = sender with { IsBanned = true };
            await _repo.UpdateSenderAsync(sender, ct).ConfigureAwait(false);

            string text = $"User { sender.Username } got banned.";
            await _bot.AnswerCallbackQueryAsync(callbackQueryId, text, cancellationToken: ct).ConfigureAwait(false);
        }

        private async Task NotifyAsync(ReviewLite review, int userId, CancellationToken ct)
        {
            Suggestion suggestion = await _repo.GetSuggestionAsync(review.SuggestionId, ct).ConfigureAwait(false);

            string reply = review.Result switch
            {
                ReviewResult.Approved => Reply.Approved,
                _ => string.Format(Reply.Declined, review.Result.ToDescription())
            };

            await _bot.SendDocumentAsync(userId, new(suggestion.FileId), reply, cancellationToken: ct).ConfigureAwait(false);
        }
    }
}
