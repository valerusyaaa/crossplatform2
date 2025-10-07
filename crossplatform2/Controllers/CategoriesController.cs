using crossplatform2.Data;
using crossplatform2.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace crossplatform2.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Categories
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CategoryResponseDto>>> GetCategories()
        {
            var categories = await _context.Categories
                .Include(c => c.Products)
                .Select(c => new CategoryResponseDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    ProductCount = c.Products.Count,
                    Products = c.Products.Select(p => new ProductSimpleDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Price = p.Price,
                        StockQuantity = p.StockQuantity,
                        IsAvailable = p.StockQuantity > 0
                    }).ToList()
                })
                .ToListAsync();

            return Ok(categories);
        }

        // GET: api/Categories/5
        [HttpGet("{id}")]
        public async Task<ActionResult<CategoryResponseDto>> GetCategory(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Products)
                .Where(c => c.Id == id)
                .Select(c => new CategoryResponseDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    ProductCount = c.Products.Count,
                    Products = c.Products.Select(p => new ProductSimpleDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Price = p.Price,
                        StockQuantity = p.StockQuantity,
                        IsAvailable = p.StockQuantity > 0
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (category == null)
            {
                return NotFound(new { error = "Категория не найдена" });
            }

            return category;
        }

        // POST: api/Categories
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<CategoryResponseDto>> PostCategory(CreateCategoryDto createDto)
        {
            // Проверяем валидацию модели
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Проверяем, нет ли категории с таким же именем
            var existingCategory = await _context.Categories
                .FirstOrDefaultAsync(c => c.Name.ToLower() == createDto.Name.ToLower());

            if (existingCategory != null)
            {
                return BadRequest(new { error = "Категория с таким названием уже существует" });
            }

            var category = new Category
            {
                Name = createDto.Name.Trim(),
                Description = createDto.Description?.Trim()
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            // Возвращаем DTO ответа
            var responseDto = new CategoryResponseDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                ProductCount = 0,
                Products = new List<ProductSimpleDto>()
            };

            return CreatedAtAction("GetCategory", new { id = category.Id }, responseDto);
        }

        // PUT: api/Categories/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PutCategory(int id, UpdateCategoryDto updateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound(new { error = "Категория не найдена" });
            }

            // Проверяем, нет ли другой категории с таким же именем
            var duplicateCategory = await _context.Categories
                .FirstOrDefaultAsync(c => c.Name.ToLower() == updateDto.Name.ToLower() && c.Id != id);

            if (duplicateCategory != null)
            {
                return BadRequest(new { error = "Категория с таким названием уже существует" });
            }

            category.Name = updateDto.Name.Trim();
            category.Description = updateDto.Description?.Trim();

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CategoryExists(id))
                {
                    return NotFound(new { error = "Категория не найдена" });
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/Categories/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
            {
                return NotFound(new { error = "Категория не найдена" });
            }

            // Проверка на наличие связанных продуктов
            if (category.Products.Any())
            {
                var productNames = category.Products.Take(3).Select(p => p.Name).ToList();
                var message = category.Products.Count > 3
                    ? $"и еще {category.Products.Count - 3} продуктов"
                    : "";

                return BadRequest(new
                {
                    error = "Невозможно удалить категорию, которая содержит продукты",
                    message = "Сначала удалите или переместите все продукты из этой категории",
                    products = productNames,
                    totalProducts = category.Products.Count,
                    additionalInfo = message
                });
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Категория успешно удалена", deletedCategory = new { id = category.Id, name = category.Name } });
        }

        // GET: api/Categories/Summary
        [HttpGet("summary")]
        [Authorize]
        public async Task<ActionResult<object>> GetCategoriesSummary()
        {
            var summary = await _context.Categories
                .Include(c => c.Products)
                .Select(c => new
                {
                    CategoryName = c.Name,
                    TotalProducts = c.Products.Count,
                    TotalValue = c.Products.Sum(p => p.Price * p.StockQuantity),
                    AveragePrice = c.Products.Any() ? Math.Round(c.Products.Average(p => p.Price), 2) : 0,
                    AvailableProducts = c.Products.Count(p => p.StockQuantity > 0),
                    OutOfStockProducts = c.Products.Count(p => p.StockQuantity == 0)
                })
                .ToListAsync();

            return Ok(summary);
        }

        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(e => e.Id == id);
        }
    }

    // DTO классы
    public class CreateCategoryDto
    {
        [Required(ErrorMessage = "Название категории обязательно")]
        [StringLength(50, ErrorMessage = "Название категории не может превышать 50 символов")]
        public string Name { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "Описание не может превышать 200 символов")]
        public string? Description { get; set; }
    }

    public class UpdateCategoryDto
    {
        [Required(ErrorMessage = "Название категории обязательно")]
        [StringLength(50, ErrorMessage = "Название категории не может превышать 50 символов")]
        public string Name { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "Описание не может превышать 200 символов")]
        public string? Description { get; set; }
    }

    public class CategoryResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int ProductCount { get; set; }
        public List<ProductSimpleDto> Products { get; set; } = new List<ProductSimpleDto>();
    }

    public class ProductSimpleDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public bool IsAvailable { get; set; }
    }
}