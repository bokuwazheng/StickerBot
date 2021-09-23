using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;

namespace StickerBot.Services
{
    public class CallbackQueryHandler
    {
        private readonly ITelegramBotClient _botClient;
        private readonly ILogger<CallbackQueryHandler> _logger;

        public CallbackQueryHandler(ITelegramBotClient botClient, ILogger<CallbackQueryHandler> logger)
        {
            _botClient = botClient;
            _logger = logger;
        }
    }
}
