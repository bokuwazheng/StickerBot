using JournalApiClient.Data;
using JournalApiClient.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
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

        public Controller(ILogger<Controller> logger, ITelegramBotClient client, IJournalApiClient journal, Jwt jwt)
        {
            _logger = logger;
            _client = client;
            _journal = journal;

            //jwt = _journal.GetJwtAsync("123", "123").Result;
        }

        [HttpGet]
        public Task TestAsync()
        {
            _logger.LogInformation("Test");

            string token = Environment.GetEnvironmentVariable("ChatId");
            return _client.SendTextMessageAsync(new(token), "I'M ALIVE!!");
        }

        [HttpPost]
        public async Task EchoAsync([FromBody] Update update)
        {
            try
            {
                _logger.LogInformation("Echo");

                Message message = update.Message;
                string response = "";
                if (message.Type is MessageType.Text && message.Text is not null)
                {
                    if (message.Text.Contains("/status"))
                    {
                        string fileId = message.Text.Split(' ')[1];
                        string status = await _journal.GetStatusAsync(message.From.Id, fileId);
                        response = $"{ fileId } : { status }";
                    }
                    else
                    {
                        response = "Use a command or send an image";
                    }
                }
                else if (message.Type is MessageType.Document)
                {
                    string ext = Path.GetExtension(message.Document.FileName).ToLower();
                    if (ext is ".jpg" or ".png")
                    {
                        string fileId = message.Document.FileId;
                        int userId = message.From.Id;
                        await _journal.CreateEntryAsync(userId, fileId);
                        string token = Environment.GetEnvironmentVariable("ChatId");
                        await _client.SendPhotoAsync(token, new(fileId));
                    }
                    else
                        response = "Please send a .JPG or .PNG file.";
                }
                
                await _client.SendTextMessageAsync(message.Chat.Id, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        [Route("/test")]
        [HttpPost]
        public async Task Echo2Async()
        {
            try
            {
                _logger.LogInformation("Echo2");
                var jwt = await _journal.GetJwtAsync("123", "123");
                string response = "";
                ChatId token = new(Environment.GetEnvironmentVariable("ChatId"));

                string ext = Path.GetExtension("fsadfsdsfda.jpg").ToLower();
                if (ext is ".jpg" or ".png")
                {
                    string fileId = "sdfgsdfg22322sss3ss371sssssss292";
                    int userId = 7777777;
                    Suggestion ss = await _journal.CreateEntryAsync(userId, fileId);
                    //Suggestion ss = await _journal.GetSuggestionAsync(fileId);

                    //await _client.SendPhotoAsync(token, new(fileId));
                }
                else
                    response = "Please send a .JPG or .PNG file.";

                //await _client.SendTextMessageAsync(token, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }
    }
}
