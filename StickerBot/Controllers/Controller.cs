using JournalApiClient.Data;
using JournalApiClient.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace StickerBot.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class Controller : ControllerBase
    {
        private readonly ILogger<Controller> _logger;
        private readonly ITelegramBotClient _client;
        private readonly IJournalApiClient _journal;

        public Controller(ILogger<Controller> logger, ITelegramBotClient client, IJournalApiClient journal)
        {
            _logger = logger;
            _client = client;
            _journal = journal;
        }

        [HttpGet]
        public Task TestAsync()
        {
            _logger.LogInformation("Test");

            string token = Environment.GetEnvironmentVariable("ChatId");
            return _client.SendTextMessageAsync(new(token), "I'M ALIVE!!");
        }

        [HttpPost]
        public async Task HanleAsync([FromBody] Update update)
        {
            try
            {
                if (update is null)
                    return;

                _logger.LogInformation($"Received a message of type { update.Type } from { update.Message?.From?.Username }");

                Message message = update.Message;

                Task task = message.Type switch
                {
                    MessageType.Document => HandleDocumentMessageAsync(message, CancellationToken.None),
                    MessageType.Text => HandleTextMessageAsync(message, CancellationToken.None),
                    _ => null
                };

                if (task is not null)
                    await task;
                else
                    await _client.SendTextMessageAsync(message.Chat.Id, "Please send an image or enter a command.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        private async Task HandleDocumentMessageAsync(Message message, CancellationToken ct)
        {
            string ext = Path.GetExtension(message.Document.FileName).ToLower();
            if (ext is ".jpg" or ".png")
            {
                User user = message.From;
                Sender sender = new()
                {
                    UserId = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Username = user.Username
                };

                string fileId = message.Document.FileId;

                await _journal.CreateEntryAsync(sender, fileId);
                await _client.SendTextMessageAsync(message.Chat.Id, "Thank you! To get notified when your submission status changes type '/subscribe'. You can also check the status manually using '/status id' command.");

                string token = Environment.GetEnvironmentVariable("ChatId");
                await _client.SendPhotoAsync(token, new(fileId));
            }
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
                "/status" => HandleStatusCommandAsync(argument),
                "/subscribe" => HandleSubscribeCommandAsync(),
                _ => null
            };

            if (task is not null)
            {
                string response = await task.ConfigureAwait(false);
                await _client.SendTextMessageAsync(message.Chat.Id, response).ConfigureAwait(false);
            }
        }

        private async Task<string> HandleStatusCommandAsync(string fileId)
        {
            string status = await _journal.GetStatusAsync(fileId).ConfigureAwait(false);
            return $"{ fileId } : { status }";
        }

        private async Task<string> HandleSubscribeCommandAsync()
        {
            await _journal.SubscribeAsync().ConfigureAwait(false);
            return "You will receive a notification once your submission status is changed.";
        }

        [Route("/test")]
        [HttpPost]
        public async Task Echo2Async()
        {
            try
            {
                _logger.LogInformation("Echo2");

                var status = await HandleStatusCommandAsync("123").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }
    }
}
