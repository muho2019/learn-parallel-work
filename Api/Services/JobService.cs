using Api.Data;
using Api.Entities;
using Api.Entities.ValueTypes;
using Api.ServiceDefaults;
using Hangfire;

namespace Api.Services
{
    public class JobService : IJobService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<JobService> _logger;
        private readonly AppDbContext _appDbContext;

        public JobService(IHttpClientFactory httpClientFactory, ILogger<JobService> logger, AppDbContext appDbContext)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _appDbContext = appDbContext;
        }

        // Hangfire가 이 메서드를 백그라운드에서 호출합니다.
        [AutomaticRetry(Attempts = 3)] // 실패 시 3번만 재시도
        public async Task ProcessJobRequestAsync(int jobId, CancellationToken ct)
        {
            _logger.LogInformation("[Job {JobId}] 작업 상태 검사", jobId);
            
            Job? job = _appDbContext.Jobs.FirstOrDefault(j => j.Id == jobId);

            if (job is null)
            {
                _logger.LogWarning("[Job {JobId}] 존재하지 않는 Job Id", jobId);
                return;
            }
            if (job.Status == Status.InProgress || job.Status == Status.Completed)
            {
                _logger.LogWarning("[Job {JobId}] 유효하지 않은 상태. [{Status}]", jobId, job.Status.ToString());
                return;
            }

            _logger.LogInformation("[Job {JobId}] 작업 시작", jobId);

            // 1. DB에서 배치 ID에 해당하는 요청 데이터 목록 조회 (가정)
            // var requests = await _repository.GetRequestsByBatchIdAsync(batchId);
            var requests = Enumerable.Range(1, 300).ToList();

            // 2. Rate Limiter가 적용된 HttpClient 생성
            var httpClient = _httpClientFactory.CreateClient(ServiceNames.Destination);

            // 3. Parallel.ForEachAsync로 제어된 병렬 처리
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = 10,
                CancellationToken = ct // Hangfire가 종료 신호를 보내면 여기서 감지
            };

            try
            {
                await Parallel.ForEachAsync(requests, parallelOptions, async (req, cs) =>
                {
                    // RateLimiter에 의해 자동으로 Throttling됨
                    var response = await httpClient.GetAsync($"/api/job-test?req={req}", cs);

                    if (response.IsSuccessStatusCode)
                    {
                        // 성공 로직 (DB 업데이트 등)
                        _logger.LogDebug("Request {Req} Success", req);
                    }
                    else
                    {
                        // 실패 로직
                        _logger.LogWarning("Request {Req} Failed: {StatusCode}", req, response.StatusCode);
                    }
                });

                job.Status = Status.Completed;
                _logger.LogInformation("[Job {JobId}] 작업 완료", jobId);
            }
            catch (Exception ex)
            {
                job.Status = Status.Failed;
                _logger.LogError(ex, "[Job {JobId}] Error", jobId);
            }

            _appDbContext.Jobs.Update(job);
            await _appDbContext.SaveChangesAsync();
        }
    }
}
