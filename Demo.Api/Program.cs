using Demo.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace Demo.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            // 1. Register SQL Server
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer")));
            // 2. Register Redis as the L2 distributed cache for HybridCache
            builder.Services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = builder.Configuration.GetConnectionString("Redis");
                options.InstanceName = "Demo:";
            });
            // 3. Register HybridCache (automatically uses the Redis above as L2)
            builder.Services.AddHybridCache(options =>
            {
                options.DefaultEntryOptions = new HybridCacheEntryOptions
                {
                    Expiration = TimeSpan.FromMinutes(5),      // L2 (Redis) expiration
                    LocalCacheExpiration = TimeSpan.FromMinutes(2) // L1 (memory) expiration
                };
            });
            // Add services to the container.
            builder.Services.AddControllers();
            var app = builder.Build();
            // Configure the HTTP request pipeline.
            app.UseHttpsRedirection();
            app.UseAuthorization();
            // Auto-create database (for demo only; production should use migrations)
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.EnsureCreated();
            }
            app.MapControllers();
            app.Run();
        }
    }
}
