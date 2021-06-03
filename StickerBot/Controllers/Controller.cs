using JournalApiClient.Data;
using JournalApiClient.Data.Enums;
using JournalApiClient.Services;
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
                    UpdateType.InlineQuery => HandleCallbackQuery(update.CallbackQuery, CancellationToken.None),
                    _ => null
                };

                if (task is not null)
                    await task;
                else
                    await _bot.SendTextMessageAsync(update.Message.Chat.Id, "Please send an image or enter a command.");
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
            if (ext is ".jpg" or ".png")
                return;

            User user = message.From;
            Sender sender = new()
            {
                UserId = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Username = user.Username
            };

            string fileId = message.Document.FileId;

            Suggestion suggestion = await _repo.CreateEntryAsync(sender, fileId);
            await _bot.SendTextMessageAsync(message.Chat.Id, "Thank you! To get notified when your submission status changes type '/subscribe'. You can also check the status manually using '/status id' command.");

            string token = Environment.GetEnvironmentVariable("ChatId");
            Review review = new() { UserId = user.Id };
            string approved = JsonConvert.SerializeObject(review.Status = Status.Approved);
            string declined = JsonConvert.SerializeObject(review.Status = Status.Declined);
            string considered = JsonConvert.SerializeObject(review.Status = Status.Review);
            string banned = JsonConvert.SerializeObject(review.Status = Status.Banned);

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

            await _bot.SendPhotoAsync(token, new(fileId), "Choose", replyMarkup: markup);
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

        private async Task<string> HandleStatusCommandAsync(string fileId, CancellationToken ct)
        {
            string status = await _repo.GetStatusAsync(fileId).ConfigureAwait(false);
            return $"{ fileId } : { status }";
        }

        private async Task<string> HandleSubscribeCommandAsync(CancellationToken ct)
        {
            await _repo.SubscribeAsync(ct).ConfigureAwait(false);
            return "You will receive a notification once your submission status is changed.";
        }

        private async Task HandleCallbackQuery(CallbackQuery callbackQuery, CancellationToken ct)
        {
            Review review = JsonConvert.DeserializeObject<Review>(callbackQuery.Data);

            Task task = review.Status switch
            {
                Status.Review => GetNextAsync(ct),
                Status.Banned => BanAsync(review.UserId, ct),
                Status.Approved or Status.Declined when review.Comment is not null => NotifyAsync(review.FileId, ct),
                Status.Approved or Status.Declined => SendCommentKeyboardAsync(review, ct),
                _ => null
            };

            await _bot.AnswerCallbackQueryAsync(callbackQuery.Id, "").ConfigureAwait(false);
        }

        private async Task GetNextAsync(CancellationToken ct)
        {
            Suggestion suggestion = await _repo.GetNewSuggestionAsync(ct).ConfigureAwait(false);
            
        }

        private async Task BanAsync(int userId, CancellationToken ct)
        {

        }

        private async Task NotifyAsync(string fileId, CancellationToken ct)
        {

        }

        private async Task SendCommentKeyboardAsync(Review review, CancellationToken ct)
        {
            string comment = Comment.DoesNotFit;
        }

        [Route("/test")]
        [HttpPost]
        public async Task Echo2Async()
        {
            try
            {
                _logger.LogInformation("Echo2");

                string token = Environment.GetEnvironmentVariable("ChatId");

                InlineKeyboardMarkup markup = new(new[]
{
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Approve", Status.Approved.ToString()),
                        InlineKeyboardButton.WithCallbackData("Decline", Status.Declined.ToString()),
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Ask later", Status.Review.ToString()),
                        InlineKeyboardButton.WithCallbackData("Ban", "Ban"),
                    }
                });

                await _bot.SendTextMessageAsync(token, "Choose", replyMarkup: markup);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }
    }
}
