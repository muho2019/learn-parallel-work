using Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Product> Products { get; set; }
        public DbSet<Job> Jobs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 추가적인 모델 설정이 필요한 경우 여기에 작성
            modelBuilder.Entity<Product>()
                .Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(100);

            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .IsRequired();

            modelBuilder.Entity<Job>()
                .Property(j => j.Title)
                .HasMaxLength(200);

            modelBuilder.Entity<Job>()
                .Property(j => j.Description)
                .HasMaxLength(1000);

            modelBuilder.Entity<Job>()
                .Property(j => j.Status)
                .HasConversion<string>();
        }
    }
}
