using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
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

        public Controller(ILogger<Controller> logger, ITelegramBotClient client)
        {
            _logger = logger;
            _client = client;
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

                var message = update.Message;
                if (message.Type is MessageType.Text)
                    await _client.SendTextMessageAsync(message.Chat.Id, message.Text);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }
    }
}
