using Demo.Shared;
using Microsoft.EntityFrameworkCore;

namespace Demo.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<Product> Products => Set<Product>();
    }
}
