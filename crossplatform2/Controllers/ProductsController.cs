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
    public class ProductsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Products
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductResponseDto>>> GetProducts()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Select(p => new ProductResponseDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    StockQuantity = p.StockQuantity,
                    CategoryId = p.CategoryId,
                    Category = p.Category != null ? p.Category.Name : "No Category",
                    IsAvailable = p.StockQuantity > 0
                })
                .ToListAsync();

            return Ok(products);
        }

        // GET: api/Products/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ProductResponseDto>> GetProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.Id == id)
                .Select(p => new ProductResponseDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    StockQuantity = p.StockQuantity,
                    CategoryId = p.CategoryId,
                    Category = p.Category != null ? p.Category.Name : "No Category",
                    IsAvailable = p.StockQuantity > 0
                })
                .FirstOrDefaultAsync();

            if (product == null)
            {
                return NotFound(new { error = "Продукт не найден" });
            }

            return product;
        }

        // POST: api/Products
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ProductResponseDto>> PostProduct(CreateProductDto createDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Проверяем существование категории
            var categoryExists = await _context.Categories.AnyAsync(c => c.Id == createDto.CategoryId);
            if (!categoryExists)
            {
                return BadRequest(new { error = "Указанная категория не существует" });
            }

            // Проверяем, нет ли продукта с таким же названием
            var existingProduct = await _context.Products
                .FirstOrDefaultAsync(p => p.Name.ToLower() == createDto.Name.ToLower());

            if (existingProduct != null)
            {
                return BadRequest(new { error = "Продукт с таким названием уже существует" });
            }

            var product = new Product
            {
                Name = createDto.Name.Trim(),
                Price = createDto.Price,
                StockQuantity = createDto.StockQuantity,
                CategoryId = createDto.CategoryId
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // Загружаем категорию для ответа
            await _context.Entry(product)
                .Reference(p => p.Category)
                .LoadAsync();

            var responseDto = new ProductResponseDto
            {
                Id = product.Id,
                Name = product.Name,
                Price = product.Price,
                StockQuantity = product.StockQuantity,
                CategoryId = product.CategoryId,
                Category = product.Category?.Name ?? "No Category",
                IsAvailable = product.IsAvailable
            };

            return CreatedAtAction("GetProduct", new { id = product.Id }, responseDto);
        }

        // PUT: api/Products/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PutProduct(int id, UpdateProductDto updateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound(new { error = "Продукт не найден" });
            }

            // Проверяем существование категории
            var categoryExists = await _context.Categories.AnyAsync(c => c.Id == updateDto.CategoryId);
            if (!categoryExists)
            {
                return BadRequest(new { error = "Указанная категория не существует" });
            }

            // Проверяем, нет ли другого продукта с таким же названием
            var duplicateProduct = await _context.Products
                .FirstOrDefaultAsync(p => p.Name.ToLower() == updateDto.Name.ToLower() && p.Id != id);

            if (duplicateProduct != null)
            {
                return BadRequest(new { error = "Продукт с таким названием уже существует" });
            }

            product.Name = updateDto.Name.Trim();
            product.Price = updateDto.Price;
            product.StockQuantity = updateDto.StockQuantity;
            product.CategoryId = updateDto.CategoryId;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(id))
                {
                    return NotFound(new { error = "Продукт не найден" });
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/Products/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound(new { error = "Продукт не найден" });
            }

            // Проверяем, нет ли заказов с этим продуктом
            var hasOrderItems = await _context.OrderItems.AnyAsync(oi => oi.ProductId == id);
            if (hasOrderItems)
            {
                return BadRequest(new
                {
                    error = "Невозможно удалить продукт, который присутствует в заказах",
                    message = "Сначала удалите все упоминания этого продукта из заказов"
                });
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Продукт успешно удален", deletedProduct = new { id = product.Id, name = product.Name } });
        }

        // GET: api/Products/Available
        [HttpGet("available")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<ProductResponseDto>>> GetAvailableProducts()
        {
            var availableProducts = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.StockQuantity > 0)
                .Select(p => new ProductResponseDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    StockQuantity = p.StockQuantity,
                    CategoryId = p.CategoryId,
                    Category = p.Category != null ? p.Category.Name : "No Category",
                    IsAvailable = true
                })
                .ToListAsync();

            return Ok(availableProducts);
        }

        // POST: api/Products/5/Increase-Stock
        [HttpPost("{id}/increase-stock")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> IncreaseStock(int id, [FromBody] int quantity)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound(new { error = "Продукт не найден" });
            }

            try
            {
                product.IncreaseStock(quantity);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = $"Запас увеличен на {quantity}",
                    productId = product.Id,
                    productName = product.Name,
                    oldQuantity = product.StockQuantity - quantity,
                    newQuantity = product.StockQuantity,
                    isAvailable = product.IsAvailable
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // POST: api/Products/5/Decrease-Stock
        [HttpPost("{id}/decrease-stock")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DecreaseStock(int id, [FromBody] int quantity)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound(new { error = "Продукт не найден" });
            }

            try
            {
                product.DecreaseStock(quantity);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = $"Запас уменьшен на {quantity}",
                    productId = product.Id,
                    productName = product.Name,
                    oldQuantity = product.StockQuantity + quantity,
                    newQuantity = product.StockQuantity,
                    isAvailable = product.IsAvailable
                });
            }
            catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // GET: api/Products/Search?name={name}
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<ProductResponseDto>>> SearchProducts([FromQuery] string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return BadRequest(new { error = "Параметр поиска не может быть пустым" });
            }

            var products = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.Name.ToLower().Contains(name.ToLower()))
                .Select(p => new ProductResponseDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    StockQuantity = p.StockQuantity,
                    CategoryId = p.CategoryId,
                    Category = p.Category != null ? p.Category.Name : "No Category",
                    IsAvailable = p.StockQuantity > 0
                })
                .ToListAsync();

            return Ok(products);
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }

        // DTO для создания продукта
        public class CreateProductDto
        {
            [Required(ErrorMessage = "Название продукта обязательно")]
            [StringLength(100, ErrorMessage = "Название продукта не может превышать 100 символов")]
            public string Name { get; set; } = string.Empty;

            [Required(ErrorMessage = "Цена продукта обязательна")]
            [Range(0.01, double.MaxValue, ErrorMessage = "Цена должна быть положительной")]
            public decimal Price { get; set; }

            [Required(ErrorMessage = "Количество на складе обязательно")]
            [Range(0, int.MaxValue, ErrorMessage = "Количество не может быть отрицательным")]
            public int StockQuantity { get; set; }

            [Required(ErrorMessage = "ID категории обязателен")]
            [Range(1, int.MaxValue, ErrorMessage = "Необходимо указать существующую категорию")]
            public int CategoryId { get; set; }
        }

        // DTO для обновления продукта
        public class UpdateProductDto
        {
            [Required(ErrorMessage = "Название продукта обязательно")]
            [StringLength(100, ErrorMessage = "Название продукта не может превышать 100 символов")]
            public string Name { get; set; } = string.Empty;

            [Required(ErrorMessage = "Цена продукта обязательна")]
            [Range(0.01, double.MaxValue, ErrorMessage = "Цена должна быть положительной")]
            public decimal Price { get; set; }

            [Required(ErrorMessage = "Количество на складе обязательно")]
            [Range(0, int.MaxValue, ErrorMessage = "Количество не может быть отрицательным")]
            public int StockQuantity { get; set; }

            [Required(ErrorMessage = "ID категории обязателен")]
            [Range(1, int.MaxValue, ErrorMessage = "Необходимо указать существующую категорию")]
            public int CategoryId { get; set; }
        }

        // DTO для ответа (чтение продукта)
        public class ProductResponseDto
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public decimal Price { get; set; }
            public int StockQuantity { get; set; }
            public int CategoryId { get; set; }
            public string Category { get; set; } = string.Empty;
            public bool IsAvailable { get; set; }
        }
    }

}