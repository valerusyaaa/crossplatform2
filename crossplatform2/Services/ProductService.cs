using Microsoft.EntityFrameworkCore;
using crossplatform2.Data;
using crossplatform2.Models;
using crossplatform2.Controllers;

namespace crossplatform2.Services
{
    public class ProductService
    {
        private readonly ApplicationDbContext _context;

        public ProductService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Product>> GetProductsAsync()
        {
            return await _context.Products
                .Include(p => p.Category)
                .ToListAsync();
        }

        public async Task<List<Product>> GetAvailableProductsAsync()
        {
            return await _context.Products
                .Include(p => p.Category)
                .Where(p => p.StockQuantity > 0)
                .ToListAsync();
        }

        public async Task<List<Product>> SearchProductsAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return new List<Product>();

            return await _context.Products
                .Include(p => p.Category)
                .Where(p => p.Name.ToLower().Contains(name.ToLower()))
                .ToListAsync();
        }

        public async Task<Product?> GetProductAsync(int id)
        {
            return await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<(bool success, string error, Product? product)> CreateProductAsync(CreateProductDto createDto)
        {
            // Проверяем существование категории
            if (!await _context.Categories.AnyAsync(c => c.Id == createDto.CategoryId))
                return (false, "CATEGORY_NOT_FOUND", null);

            // Проверяем дубликат названия
            if (await _context.Products.AnyAsync(p => p.Name.ToLower() == createDto.Name.ToLower()))
                return (false, "PRODUCT_DUPLICATE_NAME", null);

            var product = new Product
            {
                Name = createDto.Name.Trim(),
                Price = createDto.Price,
                StockQuantity = createDto.StockQuantity,
                CategoryId = createDto.CategoryId
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            await _context.Entry(product)
                .Reference(p => p.Category)
                .LoadAsync();

            return (true, string.Empty, product);
        }

        public async Task<(bool success, string error)> UpdateProductAsync(int id, UpdateProductDto updateDto)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return (false, "PRODUCT_NOT_FOUND");

            if (!await _context.Categories.AnyAsync(c => c.Id == updateDto.CategoryId))
                return (false, "CATEGORY_NOT_FOUND");

            if (await _context.Products.AnyAsync(p => p.Name.ToLower() == updateDto.Name.ToLower() && p.Id != id))
                return (false, "PRODUCT_DUPLICATE_NAME");

            product.Name = updateDto.Name.Trim();
            product.Price = updateDto.Price;
            product.StockQuantity = updateDto.StockQuantity;
            product.CategoryId = updateDto.CategoryId;

            await _context.SaveChangesAsync();
            return (true, string.Empty);

        }

        public async Task<(bool success, string error)> DeleteProductAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return (false, "PRODUCT_NOT_FOUND");

            var hasOrderItems = await _context.OrderItems.AnyAsync(oi => oi.ProductId == id);
            if (hasOrderItems)
                return (false, "PRODUCT_HAS_ORDERS");

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return (true, string.Empty);
        }

        public async Task<(bool success, string error)> IncreaseStockAsync(int id, int quantity)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return (false, "PRODUCT_NOT_FOUND");

            var (success, error) = product.IncreaseStock(quantity);
            if (!success)
                return (false, error);

            await _context.SaveChangesAsync();
            return (true, string.Empty);
        }
 
        public async Task<(bool success, string error)> DecreaseStockAsync(int id, int quantity)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return (false, "PRODUCT_NOT_FOUND");

            var (success, error) = product.DecreaseStock(quantity);
            if (!success)
                return (false, error);
            await _context.SaveChangesAsync();
            return (true, string.Empty);
        }
    }
}
