using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StickerBot.Options;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;

namespace StickerBot.Handlers
{
    public class ExceptionHandler
    {
        private readonly RequestDelegate _next;
        private readonly ITelegramBotClient _bot;
        private readonly ILogger<ExceptionHandler> _logger;
        private readonly BotOptions _botOptions;

        public ExceptionHandler(RequestDelegate next, ITelegramBotClient bot, ILogger<ExceptionHandler> logger, IOptions<BotOptions> botOptions)
        {
            _next = next;
            _bot = bot;
            _logger = logger;
            _botOptions = botOptions.Value;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                _logger.LogError("Something went wrong: {ex}", ex);

                using MemoryStream ms = new(Encoding.UTF8.GetBytes(ex.StackTrace));
                await _bot.SendDocumentAsync(_botOptions.ChatId, new(ms, "stacktrace.txt"), ex.Message);
            }
        }
    }
}
