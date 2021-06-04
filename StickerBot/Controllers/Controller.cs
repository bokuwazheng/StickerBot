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

                _logger.LogInformation($"Received a message of type { update.Type } from { update.Message?.From?.Username }");

                Task task = update.Type switch
                {
                    UpdateType.Message => HandleMessageAsync(update.Message, CancellationToken.None),
                    UpdateType.CallbackQuery => HandleCallbackQuery(update.CallbackQuery, CancellationToken.None),
                    _ => null
                };

                if (task is not null)
                    await task;
                else
                    await _bot.SendTextMessageAsync(update.Message.Chat.Id, Reply.WrongUpdateType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        private async Task HandleMessageAsync(Message message, CancellationToken ct)
        {
            Task task = message.Type switch
            {
                MessageType.Document => HandleDocumentMessageAsync(message, CancellationToken.None),
                MessageType.Text => HandleTextMessageAsync(message, CancellationToken.None),
                _ => null
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
            await _bot.SendTextMessageAsync(message.Chat.Id, $"Id { suggestion.Id }");
            await _bot.SendTextMessageAsync(message.Chat.Id, Reply.ThankYou);

            await SendForReviewAsync(user, suggestion.Id.Value, fileId, ct).ConfigureAwait(false);
        }

        private async Task SendForReviewAsync(User user, int id, string fileId, CancellationToken ct)
        {
            ChatId chat = new(Environment.GetEnvironmentVariable("ChatId"));
            ReviewLite review = new() { id = id };

            review.st = Status.Approved;
            string approved = JsonConvert.SerializeObject(review);

            review.st = Status.Declined;
            string declined = JsonConvert.SerializeObject(review);

            review.st = Status.Review;
            string considered = JsonConvert.SerializeObject(review);

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
                    InlineKeyboardButton.WithCallbackData("Next", considered),
                }
            });

            await _bot.SendDocumentAsync(chat, new(fileId), $"From { user.Username }", replyMarkup: markup, cancellationToken: ct);
        }

        private async Task HandleTextMessageAsync(Message message, CancellationToken ct)
        {
            string command = null;
            string argument = null;

            if (message.Text.StartsWith('/'))
            {
                string[] msg = message.Text.Split(' ');
                command = msg[0];
                argument = msg[1];
            }

            async Task<string> HandleStatusCommandAsync(string id, CancellationToken ct)
            {
                var parsed = int.TryParse(id, out int sug);
                if (parsed)
                {
                    string status = await _repo.GetStatusAsync(sug, ct).ConfigureAwait(false);
                    return $"{ id } : { status }";
                }
                else
                    return "";
            }

            async Task<string> HandleSubscribeCommandAsync(CancellationToken ct)
            {
                await _repo.SubscribeAsync(ct).ConfigureAwait(false);
                return Reply.Subscribed;
            }

            Task<string> task = command switch
            {
                "/status" => HandleStatusCommandAsync(argument, ct),
                "/subscribe" => HandleSubscribeCommandAsync(ct),
                _ => null
            };

            if (task is not null)
            {
                string response = await task.ConfigureAwait(false);
                await _bot.SendTextMessageAsync(message.Chat.Id, response).ConfigureAwait(false);
            }
        }

        private async Task HandleCallbackQuery(CallbackQuery callbackQuery, CancellationToken ct)
        {
            ReviewLite review = JsonConvert.DeserializeObject<ReviewLite>(callbackQuery.Data);

            Task task = review.st switch
            {
                Status.Review => GetNextAsync(ct),
                Status.Banned => BanAsync(callbackQuery.Id, review.id.Value, ct),
                Status.Approved => NotifyAsync(review, ct),
                Status.Declined when review.cm is not null => NotifyAsync(review, ct),
                Status.Declined => SendCommentKeyboardAsync(callbackQuery, review, ct),
                _ => null
            };

            await task;
        }

        private async Task GetNextAsync(CancellationToken ct)
        {
            Suggestion suggestion = await _repo.GetNewSuggestionAsync(ct).ConfigureAwait(false);
            //await SendForReviewAsync(user, fileId, ct).ConfigureAwait(false);
        }

        private async Task BanAsync(string callbackQueryId, int suggestionId, CancellationToken ct)
        {
            Sender sender = await _repo.BanAsync(suggestionId, ct).ConfigureAwait(false);
            await _bot.AnswerCallbackQueryAsync(callbackQueryId, $"User { sender.Username } got banned.", cancellationToken: ct).ConfigureAwait(false);
        }

        private async Task NotifyAsync(ReviewLite review, CancellationToken ct)
        {
            Sender sender = await _repo.GetSuggesterAsync(review.id.Value, ct).ConfigureAwait(false);

            string message = review.st switch
            {
                Status.Approved => Reply.Approved,
                Status.Declined => $"{ Reply.Declined } Reason: { review.cm }",
                _ => throw new($"Incorrect status: { review.st }. Can't notify user about unreviewed suggestion.")
            };

            sender.Notify = true;

            if (sender.Notify)
            {
                Suggestion suggestion = await _repo.GetSuggestionAsync(review.id.Value).ConfigureAwait(false);
                ChatId chat = new(sender.ChatId);
                await _bot.SendDocumentAsync(chat, new(suggestion.FileId), message, cancellationToken: ct).ConfigureAwait(false);
            }
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
            
            Sender sender = await _repo.GetSuggesterAsync(review.id.Value, ct).ConfigureAwait(false);
            ChatId chat = new(sender.ChatId);
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
