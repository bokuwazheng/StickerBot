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

namespace StickerBot.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class Controller : ControllerBase
    {
        private readonly ILogger<Controller> _logger;
        private readonly ITelegramBotClient _bot;
        private readonly IJournalApiClient _repo;

        public Controller(ILogger<Controller> logger, ITelegramBotClient bot, IJournalApiClient repo)
        {
            _logger = logger;
            _bot = bot;
            _repo = repo;
        }

        [HttpGet]
        public Task TestGetAsync()
        {
            _logger.LogInformation("Test GET");

            string token = Environment.GetEnvironmentVariable("ChatId");
            return _bot.SendTextMessageAsync(new(token), "I'M ALIVE!!");
        }


        [Route("/test")]
        [HttpPost]
        public async Task TestPostAsync()
        {
            _logger.LogInformation("Test POST");

            try
            {
                await GetNextAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        [HttpPost]
        public async Task HanleAsync([FromBody] Update update)
        {
            try
            {
                if (update is null)
                    return;

                _logger.LogInformation($"Received an update of type { update.Type }");

                User user = update.Type switch
                {
                    UpdateType.Message => update.Message.From,
                    UpdateType.CallbackQuery => update.CallbackQuery.From,
                    _ => null
                };

                if (user is null || user.IsBot)
                    return;

                Sender sender = await _repo.GetSenderAsync(user.Id).ConfigureAwait(false);

                if (sender is { IsBanned: true })
                    return;

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

                Task task = update.Type switch
                {
                    UpdateType.Message => HandleMessageAsync(update.Message, CancellationToken.None),
                    UpdateType.CallbackQuery => HandleCallbackQueryAsync(update.CallbackQuery, CancellationToken.None),
                    _ => null
                };

                if (task is not null)
                    await task.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
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
            int chatId = int.Parse(Environment.GetEnvironmentVariable("ChatId"));
            ReviewLite model = new(suggestion.Id, chatId);
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
            await _bot.SendDocumentAsync(chatId, new(suggestion.FileId), $"From { sender.Username }", replyMarkup: markup, cancellationToken: ct);
        }

        private async Task HandleTextMessageAsync(Message message, CancellationToken ct)
        {
            string command = null;
            string argument = null;

            bool match = Regex.IsMatch(message.Text, @"^\/[a-z]");

            if (match)
            {
                string[] words = message.Text.Split(' ');
                command = words[0];
                argument = words.Length > 1 ? words[1] : null;
            }

            var guidelinesUrl = Environment.GetEnvironmentVariable("Guidelines");

            Task<string> task = command switch
            {
                "/start" => Task.FromResult($"{ Reply.Hello } { Environment.NewLine } { guidelinesUrl }"),
                "/status" => GetStatusAsync(argument, message.From.Id, ct),
                "/subscribe" => ToggleSubscriptionAsync(true, message.From.Id, ct),
                "/unsubscribe" => ToggleSubscriptionAsync(false, message.From.Id, ct),
                "/guidelines" => Task.FromResult(guidelinesUrl),
                _ => Task.FromResult(Reply.WrongUpdateType)
            };

            string response = await task.ConfigureAwait(false);
            await _bot.SendTextMessageAsync(message.Chat.Id, response, cancellationToken: ct).ConfigureAwait(false);
        }

        private async Task<string> GetStatusAsync(string id, int userId, CancellationToken ct)
        {
            bool bySuggestionId = int.TryParse(id, out int suggestionId);

            if (bySuggestionId)
            {
                Suggestion suggestion = await _repo.GetSuggestionAsync(suggestionId, ct).ConfigureAwait(false);

                if (suggestion is null)
                    return string.Format(Reply.SuggestionNotFound, suggestionId);

                if (suggestion.UserId != userId)
                    return Reply.StatusUnavaliable;

                Review review = await _repo.GetReviewAsync(suggestionId, ct).ConfigureAwait(false);

                return review is null || review.ResultCode is ReviewResult.None
                    ? Reply.NotYetReviewed
                    : string.Format(Reply.Status, id, review.ResultCode.ToDescription());
            }
            else // TODO: Find sender's last submission and then look for review to be able to tell 'no submission' case from 'not reviewed' case. ???
            {
                Review review = await _repo.GetNewReviewAsync(userId, ct).ConfigureAwait(false);

                string result = review is null
                    ? Reply.NoSubmissionsOrNotReviewed
                    : string.Format(Reply.Status, review.SuggestionId, review.ResultCode.ToDescription());

                return string.Format(Reply.UseStatusN, result);
            }
        }

        private async Task<string> ToggleSubscriptionAsync(bool notify, int userId, CancellationToken ct)
        {
            Sender sender = await _repo.GetSenderAsync(userId, ct).ConfigureAwait(false);

            if (sender.Notify == notify)
                return notify ? Reply.AlreadySubscribed : Reply.AlreadyUnsubscribed;

            sender = sender with { Notify = notify };

            Sender updatedSender = await _repo.UpdateSenderAsync(sender, ct).ConfigureAwait(false);
            return updatedSender.Notify ? Reply.Subscribed : Reply.Unsubscribed;
        }

        private async Task HandleCallbackQueryAsync(CallbackQuery callbackQuery, CancellationToken ct)
        {
            ReviewLite review = JsonConvert.DeserializeObject<ReviewLite>(callbackQuery.Data);
            Review submitted = await _repo.AddReviewAsync(new(review), ct).ConfigureAwait(false);
            Sender sender = await _repo.GetSuggesterAsync(review.SuggestionId, ct).ConfigureAwait(false);

            Task task = review.Result switch
            {
                not (ReviewResult.None or ReviewResult.Banned) when sender.Notify => NotifyAsync(review, ct),
                ReviewResult.Banned => BanAsync(callbackQuery.Id, sender, ct),
                _ => null,
            };

            if (task is not null)
                await task.ConfigureAwait(false);

            if (review.Result is not ReviewResult.None)
                await _bot.EditMessageCaptionAsync(review.SuggesterId, callbackQuery.Message.MessageId, review.Result.ToDescription(), null, ct).ConfigureAwait(false);

            await GetNextAsync(ct).ConfigureAwait(false);
        }

        private async Task GetNextAsync(CancellationToken ct)
        {
            Suggestion suggestion = await _repo.GetNewSuggestionAsync(ct).ConfigureAwait(false);

            if (suggestion is null)
                return;

            await SendForReviewAsync(suggestion, ct).ConfigureAwait(false);
        }

        private async Task BanAsync(string callbackQueryId, Sender sender, CancellationToken ct)
        {
            sender = sender with { IsBanned = true };
            await _repo.UpdateSenderAsync(sender, ct).ConfigureAwait(false);
            await _bot.AnswerCallbackQueryAsync(callbackQueryId, $"User { sender.Username } got banned.", cancellationToken: ct).ConfigureAwait(false);
        }

        private async Task NotifyAsync(ReviewLite review, CancellationToken ct)
        {
            Sender sender = await _repo.GetSuggesterAsync(review.SuggestionId, ct).ConfigureAwait(false);

            if (!sender.Notify)
                return;

            string caption = string.Format(Reply.StatusChanged, review.SuggestionId, review.Result.ToDescription());

            Suggestion suggestion = await _repo.GetSuggestionAsync(review.SuggestionId).ConfigureAwait(false);
            await _bot.SendDocumentAsync(sender.UserId, new(suggestion.FileId), caption, cancellationToken: ct).ConfigureAwait(false);
        }
    }
}
