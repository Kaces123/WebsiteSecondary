using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Authentication.Model;
using WebApplication1.Data.Entity;

namespace WebApplication1.Data;

public class ShopDbContext : IdentityDbContext<ForumRestUser>
{
    private readonly IConfiguration _configuration;
    public DbSet<Shop> Shops { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Product> Products { get; set; }

    public ShopDbContext(IConfiguration configuration) // Prisijungimas prie DB
    {
        _configuration = configuration;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(_configuration.GetConnectionString("PostgreSQL"));
    }
}