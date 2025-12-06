using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
// Rate Limiting 설정
builder.Services.AddRateLimiter(options =>
{
    // [중요] 429 상태 코드 설정 (기본값은 503 Service Unavailable임)
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // "FixedPolicy"라는 이름의 정책 정의
    options.AddPolicy("FixedPolicy", context =>
    {
        // IP 주소를 파티션 키로 사용 (X-Forwarded-For 처리는 별도 고려 필요)
        string partitionKey = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 120,              // 분당 120개 허용
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,                 // 대기열 없음 (초과 시 즉시 429)
            AutoReplenishment = true
        });
    });

    // (선택 사항) 거부되었을 때 응답 메시지 커스텀
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsync("{\"error\": \"Too many requests. Please try again later.\"}", token);
    };
});

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

// [주의] UseRouting() 이후, MapControllers() 이전에 위치해야 함
app.UseRateLimiter();
// 전역으로 걸고 싶다면: app.MapControllers().RequireRateLimiting("FixedPolicy");

app.MapControllers();

app.Run();
