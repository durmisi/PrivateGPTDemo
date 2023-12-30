using Microsoft.AspNetCore.Mvc;
using PrivateGPTDemo.Server.Tools;

namespace PrivateGPTDemo.Server.Controllers
{
    [ApiController]
    [Route("api/copilot/v1")]
    public class CopilotController : ControllerBase
    {
        private readonly IEnumerable<IChatMessageHandler> _chatMessageHandlers;

        public CopilotController(IEnumerable<IChatMessageHandler> chatMessageHandlers)
        {
            _chatMessageHandlers = chatMessageHandlers;
        }


        [HttpPost]
        [Route("send")]
        public async Task<ActionResult> SendMessage(string message, CancellationToken ct = default)
        {

            var tasks = new List<Task>();

            foreach (var handler in _chatMessageHandlers)
            {
                tasks.Add(handler.HandleMessage(message, ct));  
            }


            await Task.WhenAll(tasks);

            return Ok();
        }


    }
}
