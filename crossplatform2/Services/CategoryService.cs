using crossplatform2.Controllers;
using crossplatform2.Data;
using crossplatform2.Models;
using Microsoft.EntityFrameworkCore;

namespace crossplatform2.Services
{
    public class CategoryService
    {
        private readonly ApplicationDbContext _context;

        public CategoryService(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<List<Category>> GetCategoriesAsync()
        {
            return await _context.Categories
                .Include(c => c.Products)
                .ToListAsync();
        }

        public async Task<Category?> GetCategoryWithProductsAsync(int id)
        {
            return await _context.Categories
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<bool> CategoryExists(string name)
        {
            return await _context.Categories
                .AnyAsync(c => c.Name.ToLower() == name.ToLower());
        }

        public async Task<bool> CategoryExists(int id)
        {
            return await _context.Categories.AnyAsync(c => c.Id == id);
        }

        public async Task<(bool success, Category? category, string? error)> CreateCategory(CreateCategoryDto createDto)
        {
            var existingCategory = await _context.Categories
                .FirstOrDefaultAsync(c => c.Name.ToLower() == createDto.Name.ToLower());

            if (existingCategory != null)
            {
                return (false, null, "A category with this name already exists");
            }

            var category = new Category
            {
                Name = createDto.Name.Trim(),
                Description = createDto.Description?.Trim()
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return (true, category, null);
        }

        public async Task<(bool success, string? error)> UpdateCategory(int id, UpdateCategoryDto updateDto)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return (false, "Category not found");
            }

            var duplicateCategory = await _context.Categories
                .FirstOrDefaultAsync(c => c.Name.ToLower() == updateDto.Name.ToLower() && c.Id != id);

            if (duplicateCategory != null)
            {
                return (false, "A category with this name already exists");
            }

            category.Name = updateDto.Name.Trim();
            category.Description = updateDto.Description?.Trim();

            try
            {
                await _context.SaveChangesAsync();
                return (true, null);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await CategoryExists(id))
                {
                    return (false, "Category not found");
                }
                else
                {
                    return (false, "Error updating the category");
                }
            }
        }
        public async Task<(bool success, string? error, List<string>? productNames, int totalProducts)> DeleteCategory(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
            {
                return (false, "Category not found", null, 0);
            }

            if (category.Products.Any())
            {
                var productNames = category.Products.Take(3).Select(p => p.Name).ToList();
                return (false, "It is not possible to delete a category that contains products", productNames, category.Products.Count);
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return (true, null, null, 0);
        }
    }
}
