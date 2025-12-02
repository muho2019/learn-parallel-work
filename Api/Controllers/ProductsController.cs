using Api.Data;
using Api.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(AppDbContext dbContext, ILogger<ProductsController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateProductAsync(CancellationToken cancellationToken)
        {
            // Product 객체 1000개 생성
            List<Product> products = [.. Enumerable.Range(1, 1000).Select(i => new Product { Name = $"Product {i}", Price = i * 10 })];

            // 외부 API 호출 후 데이터 가공. 병렬 처리. 10개씩 진행.
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = 10,
                CancellationToken = cancellationToken
            };

            try
            {
                await Parallel.ForEachAsync(products, parallelOptions, async (product, ct) =>
                {
                    _logger.LogInformation("Processing {ProductName} on Thread {ThreadId}", product.Name, Environment.CurrentManagedThreadId);
                    await CallFakeApiAsync(cancellationToken);
                    product.Price += 1000; // 외부 API 호출 후 가격 가공
                });

                // 데이터베이스에 저장
                await _dbContext.Products.AddRangeAsync(products, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
                
                return Ok(new { Message = "Products created successfully", products.Count });
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("CreateProductAsync cancelled by client");
                return BadRequest(new { Message = "Operation cancelled" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create products");
                return StatusCode(StatusCodes.Status500InternalServerError, new { Error = "Failed to create products" });
            }
        }

        private Task CallFakeApiAsync(CancellationToken cancellationToken)
        {
            // 실제 외부 API 호출 대신 100ms 지연 시뮬레이션
            return Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
        }
    }
}
