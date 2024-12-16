using Microsoft.EntityFrameworkCore;

namespace WebApplication2.Models;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) 
    { 
    }
    public DbSet<Cart> Carts { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<ProductPackaging> ProductPackaging { get; set; }
    public DbSet<ProductSpecification> ProductSpecifications { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Contact> Contacts { get; set; }
}