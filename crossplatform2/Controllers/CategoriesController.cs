using crossplatform2.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace crossplatform2.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly CategoryService _categoryService;

        public CategoriesController(CategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        // GET: api/Categories
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CategoryResponseDto>>> GetCategories()
        {
            var categories = await _categoryService.GetCategoriesAsync();
            var categoryDtos = categories.Select(c => new CategoryResponseDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                ProductCount = c.Products.Count,
                AvailableProducts = c.Products.Count(p => p.StockQuantity > 0),
            }).ToList();

            return Ok(categoryDtos);
        }

        // GET: api/Categories/5
        [HttpGet("{id}")]
        public async Task<ActionResult<CategoryResponseDto>> GetCategory(int id)
        {
            var category = await _categoryService.GetCategoryWithProductsAsync(id);
            if (category == null)
            {
                return NotFound(new { error = "Category is not found" });
            }

            var categoryDto = new CategoryResponseDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                ProductCount = category.Products.Count,
                AvailableProducts = category.Products.Count(p => p.StockQuantity > 0),
                Products = category.Products.Select(p => new ProductSimpleDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    StockQuantity = p.StockQuantity,
                    IsAvailable = p.StockQuantity > 0
                }).ToList()
            };

            return categoryDto;
        }

        // POST: api/Categories
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<CategoryResponseDto>> PostCategory(CreateCategoryDto createDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var (success, category, error) = await _categoryService.CreateCategory(createDto);
            if (!success)
            {
                return BadRequest(new { error = error });
            }

            var responseDto = new CategoryResponseDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                ProductCount = 0
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

            var (success, error) = await _categoryService.UpdateCategory(id, updateDto);
            if (!success)
            {
                return BadRequest(new { error = error });
            }

            return NoContent();
        }

        // DELETE: api/Categories/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var (success, error, productNames, totalProducts) = await _categoryService.DeleteCategory(id);

            if (!success)
            {
                if (productNames != null) // Есть связанные продукты
                {
                    var message = totalProducts > 3
                        ? $"and more {totalProducts - 3} products"
                        : "";

                    return BadRequest(new
                    {
                        error = error,
                        message = "Delete or move all products from this category.",
                        products = productNames,
                        totalProducts = totalProducts,
                        additionalInfo = message
                    });
                }

                return BadRequest(new { error = error });
            }

            return Ok();
        }
    }
    public class CreateCategoryDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class UpdateCategoryDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class CategoryResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int ProductCount { get; set; }
        public int AvailableProducts { get; set; }
        public List<ProductSimpleDto>? Products { get; set; }
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
