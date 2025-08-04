using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Vstore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        [HttpPost("ping")]
        public IActionResult Ping([FromBody] PingRequest request)
        {
            var serverReceivedTime = DateTime.UtcNow;
            var roundTripDuration = serverReceivedTime - request.ClientSentTime;

            return Ok(new
            {
                clientSentTime = request.ClientSentTime,
                serverReceivedTime,
                roundTripDurationInMilliseconds = roundTripDuration.TotalMilliseconds
            });
        }

        public class PingRequest
        {
            public DateTime ClientSentTime { get; set; }
        }
        [HttpGet("wait")]
        public async Task<IActionResult> WaitBeforeResponse()
        {
            // استني 120 ثانية (120000 ميلي ثانية)
            await Task.Delay(TimeSpan.FromSeconds(150));

            return Ok(new
            {
                message = "Response after 150 seconds delay",
                serverTime = DateTime.UtcNow
            });
        }

    }
}
