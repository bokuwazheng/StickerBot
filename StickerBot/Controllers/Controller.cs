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

        public Controller(ILogger<Controller> logger, ITelegramBotClient client, IJournalApiClient journal)
        {
            _logger = logger;
            _bot = client;
            _repo = journal;
        }

        [HttpGet]
        public Task TestAsync()
        {
            _logger.LogInformation("Test");

            string token = Environment.GetEnvironmentVariable("ChatId");
            return _bot.SendTextMessageAsync(new(token), "I'M ALIVE!!");
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
                    UpdateType.CallbackQuery => update.CallbackQuery.From, // TODO: why???
                    _ => null
                };

                if (user is null || user.IsBot)
                    return;

                Sender sender = await _repo.GetSenderAsync(user.Id).ConfigureAwait(false);

                if (sender is { IsBanned: true }) // TODO: test with sender NULL
                    return;

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

            User user = message.From;
            Sender sender = new()
            {
                UserId = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Username = user.Username,
                ChatId = message.Chat.Id
            };

            string fileId = message.Document.FileId;

            Suggestion suggestion = await _repo.CreateSuggestionAsync(sender, fileId).ConfigureAwait(false);
            string text = string.Format(Reply.ThankYou, suggestion.Id);
            await _bot.SendTextMessageAsync(message.Chat.Id, text, cancellationToken: ct).ConfigureAwait(false);

            Suggestion unreviewed = await _repo.GetNewSuggestionAsync(ct).ConfigureAwait(false);

            if (unreviewed is null)
                await SendForReviewAsync(sender, suggestion, ct).ConfigureAwait(false);
        }

        private async Task SendForReviewAsync(Sender sender, Suggestion suggestion, CancellationToken ct)
        {
            int chatId = int.Parse(Environment.GetEnvironmentVariable("ChatId"));
            ReviewLite model = new(suggestion.Id.Value, chatId);
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

            Task<string> task = command switch
            {
                "/start" => Task.FromResult($"{ Reply.Hello } { Environment.NewLine } { Environment.GetEnvironmentVariable("Guidelines") }"),
                "/status" => GetStatusAsync(argument, message.From.Id, ct),
                "/subscribe" => ToggleSubscriptionAsync(true, message.From.Id, ct),
                "/unsubscribe" => ToggleSubscriptionAsync(false, message.From.Id, ct),
                "/next" => GetNextAsync(ct),
                _ => Task.FromResult(Reply.WrongUpdateType)
            };

            string response = await task.ConfigureAwait(false);
            await _bot.SendTextMessageAsync(message.Chat.Id, response).ConfigureAwait(false);
        }

        private async Task<string> GetStatusAsync(string id, int userId, CancellationToken ct)
        {
            string reply;
            bool bySuggestionId = int.TryParse(id, out int suggestionId);

            if (bySuggestionId)
            {
                Suggestion suggestion = await _repo.GetSuggestionAsync(suggestionId, ct).ConfigureAwait(false);

                if (suggestion is null)
                    reply = string.Format(Reply.SuggestionNotFound, suggestionId);
                else if (suggestion.UserId == userId)
                {
                    Review review = await _repo.GetReviewAsync(suggestionId, ct).ConfigureAwait(false);

                    reply = review is null
                        ? Reply.NotYetReviewed
                        : $"{ id } { review.ResultCode.ToDescription() }";
                }
                else
                    reply = Reply.StatusUnavaliable;
            }
            else // TODO: Find sender's last submission and then look for review to be able to tell 'no submission' case from 'not reviewed' case. ???
            {
                Review review = await _repo.GetNewReviewAsync(userId, ct).ConfigureAwait(false);

                string result = review is null
                    ? Reply.NoSubmissionsOrNotReview
                    : $"{ id } { review.ResultCode.ToDescription() }";

                reply = string.Format(Reply.UseStatusN, result);
            }

            return reply;
        }

        private async Task<string> ToggleSubscriptionAsync(bool notify, int userId, CancellationToken ct)
        {
            Sender sender = await _repo.GetSenderAsync(userId, ct).ConfigureAwait(false);

            if (sender.Notify == notify)
                return notify ? Reply.AlreadySubscribed : Reply.AlreadyUnsubscribed;

            sender.Notify = notify;

            Sender updatedSender = await _repo.UpdateSenderAsync(sender, ct).ConfigureAwait(false);
            return updatedSender.Notify ? Reply.Subscribed : Reply.Unsubscribed;
        }

        private async Task HandleCallbackQueryAsync(CallbackQuery callbackQuery, CancellationToken ct)
        {
            ReviewLite review = JsonConvert.DeserializeObject<ReviewLite>(callbackQuery.Data);
            Review submitted = await _repo.AddReviewAsync(new(review), ct).ConfigureAwait(false);
            Sender sender = await _repo.GetSuggesterAsync(review.Id, ct).ConfigureAwait(false);

            Task task = review.Result switch
            {
                not ReviewResult.None or ReviewResult.Banned when sender.Notify => NotifyAsync(review, ct),
                ReviewResult.Banned => BanAsync(callbackQuery.Id, sender, ct),
                _ => null,
            };

            if (task is not null)
                await task.ConfigureAwait(false);

            await _bot.EditMessageCaptionAsync(review.By, callbackQuery.Message.MessageId, review.Result.ToDescription(), null, ct).ConfigureAwait(false);
            await GetNextAsync(ct).ConfigureAwait(false);
        }

        private async Task<string> GetNextAsync(CancellationToken ct)
        {
            Suggestion suggestion = await _repo.GetNewSuggestionAsync(ct).ConfigureAwait(false);
            Sender sender = await _repo.GetSuggesterAsync(suggestion.Id.Value, ct).ConfigureAwait(false);
            await SendForReviewAsync(sender, suggestion, ct).ConfigureAwait(false);
            return "NEXT";
        }

        private async Task BanAsync(string callbackQueryId, Sender sender, CancellationToken ct)
        {
            sender.IsBanned = true;
            await _repo.UpdateSenderAsync(sender, ct).ConfigureAwait(false);
            await _bot.AnswerCallbackQueryAsync(callbackQueryId, $"User { sender.Username } got banned.", cancellationToken: ct).ConfigureAwait(false);
        }

        private async Task NotifyAsync(ReviewLite review, CancellationToken ct)
        {
            Sender sender = await _repo.GetSuggesterAsync(review.Id, ct).ConfigureAwait(false);

            if (!sender.Notify)
                return;

            string comment = review.Result.ToDescription();
            string message = review.Result is ReviewResult.Approved ? Reply.Approved : Reply.Declined;
            string caption = $"{ message } { comment }";

            Suggestion suggestion = await _repo.GetSuggestionAsync(review.Id).ConfigureAwait(false);
            ChatId chat = new(sender.ChatId); // TODO: isn't it the same as using sender.UserId? 
            await _bot.SendDocumentAsync(chat, new(suggestion.FileId), caption, cancellationToken: ct).ConfigureAwait(false);
        }

        [Route("/test")]
        [HttpPost]
        public async Task Echo2Async()
        {
            try
            {
                _logger.LogInformation("Echo2");

                string token = Environment.GetEnvironmentVariable("ChatId");

                Sender sender = new()
                {
                    UserId = 123123,
                    FirstName = "Zheng",
                    LastName = null,
                    Username = "bokuwazheng",
                    ChatId = 123123
                };

                string fileId = "FILEIDIDIDD";

                Suggestion suggestion = await _repo.CreateSuggestionAsync(sender, fileId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }
    }
}
