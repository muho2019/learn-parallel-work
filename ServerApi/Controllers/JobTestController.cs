using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ServerApi.Controllers
{
    [Route("api/job-test")]
    [ApiController]
    public class JobTestController : ControllerBase
    {
        private readonly ILogger<JobTestController> _logger;

        public JobTestController(ILogger<JobTestController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        [EnableRateLimiting("FixedPolicy")]
        public async Task<IActionResult> Get([FromQuery] int req,CancellationToken cancellationToken)
        {
            _logger.LogInformation("[Req {Req}] 작업 시작", req);

            // Simulate some processing delay
            await Task.Delay(100, cancellationToken);
            return Ok();
        }
    }
}
