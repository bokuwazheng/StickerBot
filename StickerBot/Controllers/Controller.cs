using JournalApiClient.Data;
using JournalApiClient.Data.Constants;
using JournalApiClient.Data.Enums;
using JournalApiClient.Services;
using Microsoft.AspNetCore.Builder;
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
        private readonly IReviewStateService _review;

        public Controller(ILogger<Controller> logger, ITelegramBotClient client, IJournalApiClient journal, IReviewStateService review)
        {
            _logger = logger;
            _bot = client;
            _repo = journal;
            _review = review;
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
                    UpdateType.CallbackQuery => update.CallbackQuery.From,
                    _ => null
                };

                if (user is null)
                    return;

                Sender sender = await _repo.GetSenderAsync(user.Id).ConfigureAwait(false);

                if (sender.IsBanned)
                    return;

                Task task = update.Type switch
                {
                    UpdateType.Message => HandleMessageAsync(update.Message, CancellationToken.None),
                    UpdateType.CallbackQuery => HandleCallbackQuery(update.CallbackQuery, CancellationToken.None),
                    _ => null
                };

                if (task is not null)
                    await task;
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

            await task;
        }

        private async Task HandleDocumentMessageAsync(Message message, CancellationToken ct)
        {
            string ext = Path.GetExtension(message.Document.FileName).ToLower();
            if (ext is not ".jpg" or ".png")
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

            Suggestion suggestion = await _repo.CreateEntryAsync(sender, fileId);
            await _bot.SendTextMessageAsync(message.Chat.Id, string.Format(Reply.ThankYou, suggestion.Id));

            Suggestion unreviewed = await _repo.GetNewSuggestionAsync(ct).ConfigureAwait(false);

            if (unreviewed is null)
                await SendForReviewAsync(sender, suggestion, ct).ConfigureAwait(false);
        }

        private async Task SendForReviewAsync(Sender sender, Suggestion suggestion, CancellationToken ct)
        {
            ChatId chat = new(Environment.GetEnvironmentVariable("ChatId"));
            ReviewLite review = new() { id = suggestion.Id };

            review.st = Status.Approved;
            string approved = JsonConvert.SerializeObject(review);

            review.st = Status.Declined;
            string declined = JsonConvert.SerializeObject(review);

            review.st = Status.Review;
            string reviewed = JsonConvert.SerializeObject(review);

            review.st = Status.Banned;
            string banned = JsonConvert.SerializeObject(review);

            InlineKeyboardMarkup markup = new(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Approve", approved),
                    InlineKeyboardButton.WithCallbackData("Decline", declined),
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Ban", banned),
                    InlineKeyboardButton.WithCallbackData("Next", reviewed),
                }
            });

            await _bot.SendDocumentAsync(chat, new(suggestion.FileId), $"From { sender.Username }", replyMarkup: markup, cancellationToken: ct);
        }

        private async Task HandleTextMessageAsync(Message message, CancellationToken ct)
        {
            string command = null;
            string argument = null;

            bool match = Regex.IsMatch(message.Text, "^\'/[a-z]");

            if (match)
            {
                string[] words = message.Text.Split(' ');
                command = words[0];
                argument = words.Length > 1 ? words[1] : null;
            }

            Task<string> task = command switch
            {
                "/start" => Task.FromResult($"{ Reply.Hello } { Environment.GetEnvironmentVariable("Guidelines") }"),
                "/status" => HandleStatusCommandAsync(argument, message.From.Id, ct),
                "/subscribe" => HandleSubscribeCommandAsync(true, message.From.Id, ct),
                "/unsubscribe" => HandleSubscribeCommandAsync(false, message.From.Id, ct),
                _ => Task.FromResult(Reply.WrongUpdateType)
            };

            string response = await task.ConfigureAwait(false);
            await _bot.SendTextMessageAsync(message.Chat.Id, response).ConfigureAwait(false);
        }

        private async Task<string> HandleStatusCommandAsync(string id, int userId, CancellationToken ct)
        {
            bool parsed = int.TryParse(id, out int suggestionId);
            if (parsed)
            {
                Suggestion suggestion = await _repo.GetSuggestionAsync(suggestionId, ct).ConfigureAwait(false);

                if (suggestion is null)
                    return Reply.SuggestionNotFound;
                else if (suggestion.UserId == userId)
                    return $"{ id } { suggestion.Status } { suggestion.Comment }";
                else
                    return Reply.StatusUnavaliable;
            }
            else
                return string.Format(Reply.InvalidId, id);
        }

        private async Task<string> HandleSubscribeCommandAsync(bool notify, int userId, CancellationToken ct)
        {
            bool subscribed = await _repo.SubscribeAsync(userId, notify, ct).ConfigureAwait(false);
            return subscribed ? Reply.Subscribed : Reply.Unsubscribed;
        }

        private async Task HandleCallbackQuery(CallbackQuery callbackQuery, CancellationToken ct)
        {
            ReviewLite review = JsonConvert.DeserializeObject<ReviewLite>(callbackQuery.Data);

            Task task = review.st switch
            {
                Status.Review => GetNextAsync(ct),
                Status.Banned => BanAsync(callbackQuery.Id, review.id.Value, ct),
                Status.Approved => NotifyAsync(callbackQuery, review, ct),
                Status.Declined when review.cm is not null => NotifyAsync(callbackQuery, review, ct),
                Status.Declined => SendCommentKeyboardAsync(callbackQuery, review, ct),
                _ => null
            };

            await task;
        }

        private async Task GetNextAsync(CancellationToken ct)
        {
            Suggestion suggestion = await _repo.GetNewSuggestionAsync(ct).ConfigureAwait(false);
            Sender sender = await _repo.GetSuggesterAsync(suggestion.Id.Value, ct).ConfigureAwait(false);
            await SendForReviewAsync(sender, suggestion, ct).ConfigureAwait(false);
        }

        private async Task BanAsync(string callbackQueryId, int suggestionId, CancellationToken ct)
        {
            Sender sender = await _repo.BanAsync(suggestionId, ct).ConfigureAwait(false);
            await _bot.AnswerCallbackQueryAsync(callbackQueryId, $"User { sender.Username } got banned.", cancellationToken: ct).ConfigureAwait(false);
        }

        private async Task NotifyAsync(CallbackQuery callbackQuery, ReviewLite review, CancellationToken ct)
        {
            ChatId reviewChat = new(Environment.GetEnvironmentVariable("ChatId"));
            await _bot.EditMessageReplyMarkupAsync(reviewChat, callbackQuery.Message.MessageId, null, ct).ConfigureAwait(false);

            Sender sender = await _repo.GetSuggesterAsync(review.id.Value, ct).ConfigureAwait(false);

            if (!sender.Notify)
                return;

            string comment = Comment.ToList()[review.cm.Value];

            string message = review.st switch
            {
                Status.Approved => Reply.Approved,
                Status.Declined => $"{ Reply.Declined } Reason: { comment }",
                _ => throw new($"Incorrect status: { review.st }. Can't notify user about unreviewed suggestion.")
            };

            Suggestion suggestion = await _repo.GetSuggestionAsync(review.id.Value).ConfigureAwait(false);
            ChatId chat = new(sender.ChatId);
            await _bot.SendDocumentAsync(chat, new(suggestion.FileId), message, cancellationToken: ct).ConfigureAwait(false); // TODO: reply instead of send or not (what is user deletes the msg???
        }

        private async Task SendCommentKeyboardAsync(CallbackQuery callbackQuery, ReviewLite review, CancellationToken ct)
        {
            List<string> comments = Comment.ToList();
            Dictionary<string, string> map = new();

            for (int i = 0; i < comments.Count; i++)
            {
                review.cm = i;
                string cm = JsonConvert.SerializeObject(review);

                map.Add(comments[i], cm);
            }

            InlineKeyboardMarkup markup = new(map
                .Select(item => new[] { InlineKeyboardButton.WithCallbackData(item.Key, item.Value) }).ToArray());
            
            ChatId chat = new(Environment.GetEnvironmentVariable("ChatId"));
            await _bot.EditMessageReplyMarkupAsync(chat, callbackQuery.Message.MessageId, markup, ct).ConfigureAwait(false);
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

                Suggestion suggestion = await _repo.CreateEntryAsync(sender, fileId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }
    }
}
