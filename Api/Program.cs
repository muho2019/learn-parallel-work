using Api.Data;
using Api.ServiceDefaults;
using Api.Services;
using Hangfire;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using System.Net.Http;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.AddServiceDefaults();

builder.AddSqlServerDbContext<AppDbContext>(ServiceNames.Db);

// Hangfire 설정
var dbConnectionString = builder.Configuration.GetConnectionString(ServiceNames.Db);
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(dbConnectionString)); // DB에 Hangfire 전용테이블들이 자동 생성됨
builder.Services.AddHangfireServer(); // 백그라운드 서버 구동

// DI 설정
builder.Services.AddScoped<IJobService, JobService>();

// HttpClient 설정
builder.Services.AddHttpClient(ServiceNames.Destination, client =>
{
    // 중요: "https://{AppHost에서_지정한_이름}" 형식을 사용합니다.
    // 포트 번호 없이 이름만 쓰면 Aspire가 알아서 찾아줍니다.
    client.BaseAddress = new Uri("https://localhost:7146");
})
    // Rate Limiter 핸들러 추가. 1분에 50회 요청으로 제한
    .AddStandardResilienceHandler(options =>
    {
        // Customize the Rate Limiter options
        options.RateLimiter.RateLimiter = args =>
        {
            // Define and return your custom rate limiter implementation
            var limiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions()
            {
                PermitLimit = 50, // Max requests per window
                Window = TimeSpan.FromMinutes(1), // Time window duration
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 10 // Max queued requests
            });
            // Acquire a lease from the pre-defined limiter instance
            return limiter.AcquireAsync(cancellationToken: args.Context.CancellationToken);
        };
    });

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        // DB가 없으면 생성하고, 마이그레이션 적용
        await db.Database.MigrateAsync();
    }
}

app.UseHangfireDashboard();

app.Run();
