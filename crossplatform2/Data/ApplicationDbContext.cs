using Microsoft.EntityFrameworkCore;
using crossplatform2.Models;

namespace crossplatform2.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Настройка отношений
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId);

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderId);

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Product)
                .WithMany()
                .HasForeignKey(oi => oi.ProductId);

            // Начальные данные
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Electronics", Description = "Electronic devices" },
                new Category { Id = 2, Name = "Books", Description = "Books and literature" },
                new Category { Id = 3, Name = "Clothing", Description = "Clothes and accessories" },
                new Category { Id = 4, Name = "Home", Description = "Home and garden" },
                new Category { Id = 5, Name = "Sports", Description = "Sports equipment" }
            );

            modelBuilder.Entity<Product>().HasData(
                // Electronics
                new Product { Id = 1, Name = "Laptop", Price = 999.99m, StockQuantity = 10, CategoryId = 1 },
                new Product { Id = 2, Name = "Smartphone", Price = 499.99m, StockQuantity = 20, CategoryId = 1 },
                new Product { Id = 3, Name = "Tablet", Price = 299.99m, StockQuantity = 15, CategoryId = 1 },
                new Product { Id = 4, Name = "Headphones", Price = 99.99m, StockQuantity = 30, CategoryId = 1 },
                new Product { Id = 5, Name = "Smart Watch", Price = 199.99m, StockQuantity = 25, CategoryId = 1 },

                // Books
                new Product { Id = 6, Name = "Programming Book", Price = 49.99m, StockQuantity = 50, CategoryId = 2 },
                new Product { Id = 7, Name = "Science Fiction", Price = 19.99m, StockQuantity = 40, CategoryId = 2 },
                new Product { Id = 8, Name = "Cookbook", Price = 29.99m, StockQuantity = 35, CategoryId = 2 },

                // Clothing
                new Product { Id = 9, Name = "T-Shirt", Price = 24.99m, StockQuantity = 100, CategoryId = 3 },
                new Product { Id = 10, Name = "Jeans", Price = 59.99m, StockQuantity = 60, CategoryId = 3 }
            );

            modelBuilder.Entity<User>().HasData(
                new User { Id = 1, Username = "admin", Password = "admin123", Role = "Admin" },
                new User { Id = 2, Username = "user", Password = "user123", Role = "User" }
            );
        }
    
    }
}