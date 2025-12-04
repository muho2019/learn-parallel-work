using Api.Entities.ValueTypes;
using System.ComponentModel.DataAnnotations;

namespace Api.Entities
{
    public class Job
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Status Status { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public string? HangfireJobId { get; set; }
    }
}
