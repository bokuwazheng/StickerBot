using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using StickerBot.Services;

namespace StickerBot.Controllers
{
    public class WebhookController : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Update update, [FromServices] UpdateHandler updateHandler)
        {
            if (update is null)
                return BadRequest();

            await updateHandler.HandleAsync(update, CancellationToken.None);

            return Ok();
        }
    }
}