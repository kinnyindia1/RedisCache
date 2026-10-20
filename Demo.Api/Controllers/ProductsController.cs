using Demo.Api.Data;
using Demo.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Hybrid;

namespace Demo.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly HybridCache _cache;
        public ProductsController(AppDbContext db, HybridCache cache)
        {
            _db = db;
            _cache = cache;
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var cacheKey = $"product:{id}";
            // GetOrCreateAsync: fetch from L1 (memory) first -> L2 (Redis) -> SQL Server
            // Built-in stampede protection: 100 concurrent requests, only 1 queries the database
            var product = await _cache.GetOrCreateAsync(
                cacheKey,
                async token => await _db.Products.FindAsync(new object[] { id }, token),
                cancellationToken: HttpContext.RequestAborted
            );
            return product is null ? NotFound() : Ok(product);
        }
        [HttpPost]
        public async Task<IActionResult> Create(Product product)
        {
            _db.Products.Add(product);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = product.Id }, product);
        }
        // After updating/deleting, must actively invalidate the cache
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, Product input)
        {
            var product = await _db.Products.FindAsync(id);
            if (product is null) return NotFound();
            product.Name = input.Name;
            product.Price = input.Price;
            await _db.SaveChangesAsync();
            // Critical: remove old cache, otherwise users will see stale data
            await _cache.RemoveAsync($"product:{id}");
            return NoContent();
        }
     }
}
