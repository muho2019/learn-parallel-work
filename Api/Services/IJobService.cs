namespace Api.Services
{
    public interface IJobService
    {
        Task ProcessJobRequestAsync(int batchId, CancellationToken ct);
    }
}
