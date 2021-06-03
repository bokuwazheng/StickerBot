using JournalApiClient.Data;
using JournalApiClient.Data.Constants;
using JournalApiClient.Data.Enums;
using JournalApiClient.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
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

            await _repo.CreateEntryAsync(sender, fileId);
            await _bot.SendTextMessageAsync(message.Chat.Id, $"Id { fileId }");
            await _bot.SendTextMessageAsync(message.Chat.Id, Reply.ThankYou);

            await SendForReviewAsync(user, fileId, ct).ConfigureAwait(false);
        }

        private async Task SendForReviewAsync(User user, string fileId, CancellationToken ct)
        {
            ChatId chat = new(Environment.GetEnvironmentVariable("ChatId"));
            Review review = new() { UserId = user.Id };

            review.Status = Status.Approved;
            string approved = JsonConvert.SerializeObject(review);

            review.Status = Status.Declined;
            string declined = JsonConvert.SerializeObject(review);

            review.Status = Status.Review;
            string considered = JsonConvert.SerializeObject(review);

            review.Status = Status.Banned;
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

            Message messge = await _bot.SendDocumentAsync(chat, new(fileId), $"From { user.Username }", replyMarkup: markup, cancellationToken: ct);

            if (messge.Type is MessageType.Unknown)
                _logger.LogInformation("GBPLF)");
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

            async Task<string> HandleStatusCommandAsync(string fileId, CancellationToken ct)
            {
                string status = await _repo.GetStatusAsync(fileId, ct).ConfigureAwait(false);
                return $"{ fileId } : { status }";
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
            Review review = JsonConvert.DeserializeObject<Review>(callbackQuery.Data);

            Task task = review.Status switch
            {
                Status.Review => GetNextAsync(ct),
                Status.Banned => BanAsync(callbackQuery.Id, review.UserId, ct),
                Status.Approved => NotifyAsync(review, ct),
                Status.Declined when review.Comment is not null => NotifyAsync(review, ct),
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

        private async Task BanAsync(string callbackQueryId, int userId, CancellationToken ct)
        {
            await _repo.BanAsync(userId, ct).ConfigureAwait(false);
            await _bot.AnswerCallbackQueryAsync(callbackQueryId, $"User { userId } got banned.", cancellationToken: ct).ConfigureAwait(false);
        }

        private async Task NotifyAsync(Review review, CancellationToken ct)
        {
            Sender sender = await _repo.GetSenderAsync(review.UserId, ct).ConfigureAwait(false);

            string message = review.Status switch
            {
                Status.Approved => Reply.Approved,
                Status.Declined => $"{ Reply.Declined } Reason: { review.Comment }",
                _ => throw new($"Incorrect status: { review.Status }. Can't notify user about unreviewed suggestion.")
            };

            if (sender.Notify)
                await _bot.SendPhotoAsync(sender.ChatId, new(review.FileId), message, cancellationToken: ct).ConfigureAwait(false);
        }

        private async Task SendCommentKeyboardAsync(CallbackQuery callbackQuery, Review review, CancellationToken ct)
        {
            review.Comment = Comment.LowQuality;
            string comment1 = JsonConvert.SerializeObject(review);

            review.Comment = Comment.DoesNotFit;
            string comment2 = JsonConvert.SerializeObject(review);

            review.Comment = Comment.TooSimilar;
            string comment3 = JsonConvert.SerializeObject(review);

            review.Comment = Comment.Other;
            string comment4 = JsonConvert.SerializeObject(review);

            InlineKeyboardMarkup markup = new(new[]
{
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Low quality", comment1),
                    InlineKeyboardButton.WithCallbackData("Does not fit", comment2),
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Too similar", comment3),
                    InlineKeyboardButton.WithCallbackData("Other", comment4),
                }
            });

            await _bot.EditMessageReplyMarkupAsync(callbackQuery.InlineMessageId, markup, ct).ConfigureAwait(false);
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
