using Api.Data;
using Api.Entities;
using Api.Services;
using Hangfire;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ParallelController : ControllerBase
    {
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly AppDbContext _appDbContext;

        public ParallelController(IBackgroundJobClient backgroundJobClient, AppDbContext appDbContext)
        {
            _backgroundJobClient = backgroundJobClient;
            _appDbContext = appDbContext;
        }

        [HttpPost("hangfire")]
        public async Task<IActionResult> HangfireExample(CancellationToken cancellationToken)
        {
            int jobId = CreateJob();

            _backgroundJobClient.Enqueue<JobService>(service => service.ProcessJobRequestAsync(jobId, CancellationToken.None));

            return Accepted(new { BatchId = jobId });
        }

        private int CreateJob()
        {
            var job = new Job
            {
                Title = "Sample Job",
                Description = "This is a sample job created at " + DateTimeOffset.Now,
                CreatedAt = DateTimeOffset.Now
            };
            _appDbContext.Jobs.Add(job);
            _appDbContext.SaveChanges();
            return job.Id;
        }
    }
}
