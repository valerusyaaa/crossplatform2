using crossplatform2.Models;
using crossplatform2.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace crossplatform2.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly ProductService _productService;

        public ProductsController(ProductService productService)
        {
            _productService = productService;
        }

        // GET: api/Products
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductResponseDto>>> GetProducts()
        {
            var products = await _productService.GetProductsAsync();
            var productDtos = products.Select(p => MapToDto(p)).ToList();
            return Ok(productDtos);
        }

        // GET: api/Products/Available
        [HttpGet("available")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<ProductResponseDto>>> GetAvailableProducts()
        {
            var products = await _productService.GetAvailableProductsAsync();
            var productDtos = products.Select(p => MapToDto(p)).ToList();
            return Ok(productDtos);
        }

        // GET: api/Products/Search?name={name}
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<ProductResponseDto>>> SearchProducts([FromQuery] string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return BadRequest(new { error = "The search parameter cannot be empty" });
            }

            var products = await _productService.SearchProductsAsync(name);
            var productDtos = products.Select(p => MapToDto(p)).ToList();
            return Ok(productDtos);
        }

        // GET: api/Products/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ProductResponseDto>> GetProduct(int id)
        {
            var product = await _productService.GetProductAsync(id);
            if (product == null)
            {
                return NotFound(new { error = "Product not found" });
            }

            return MapToDto(product);
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

            var (success, error, product) = await _productService.CreateProductAsync(createDto);

            if (!success)
            {
                return BadRequest(new { error = GetErrorMessage(error) });
            }

            var responseDto = MapToDto(product!);
            return CreatedAtAction("GetProduct", new { id = product!.Id }, responseDto);
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

            var (success, error) = await _productService.UpdateProductAsync(id, updateDto);

            if (!success)
            {
                return BadRequest(new { error = GetErrorMessage(error) });
            }

            return NoContent();
        }

        // DELETE: api/Products/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var (success, error) = await _productService.DeleteProductAsync(id);

            if (!success)
            {
                return BadRequest(new { error = GetErrorMessage(error) });
            }

            return Ok(new { message = "The product was successfully deleted" });
        }

        // POST: api/Products/5/Increase-Stock
        [HttpPost("{id}/increase-stock")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> IncreaseStock(int id, [FromBody] int quantity)
        {
            var (success, error) = await _productService.IncreaseStockAsync(id, quantity);

            if (!success)
            {
                return BadRequest(new { error = "Failed to increase stock" });
            }

            var product = await _productService.GetProductAsync(id);
            return Ok(new
            {
                message = $"The stock has been increased by {quantity}",
                productId = product!.Id,
                productName = product.Name,
                newQuantity = product.StockQuantity,
                isAvailable = product.IsAvailable
            });
        }

        // POST: api/Products/5/Decrease-Stock
        [HttpPost("{id}/decrease-stock")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DecreaseStock(int id, [FromBody] int quantity)
        {
            var (success, error) = await _productService.DecreaseStockAsync(id, quantity);

            if (!success)
            {
                return BadRequest(new { error = GetErrorMessage(error) });
            }

            var product = await _productService.GetProductAsync(id);
            return Ok(new
            {
                message = $"The stock has been decreased by {quantity}",
                productId = product!.Id,
                productName = product.Name,
                newQuantity = product.StockQuantity,
                isAvailable = product.IsAvailable
            });
        }

        private ProductResponseDto MapToDto(Product product)
        {
            return new ProductResponseDto
            {
                Id = product.Id,
                Name = product.Name,
                Price = product.Price,
                StockQuantity = product.StockQuantity,
                CategoryId = product.CategoryId,
                Category = product.Category?.Name ?? "No Category",
                IsAvailable = product.IsAvailable
            };
        }

        private string GetErrorMessage(string errorCode)
        {
            return errorCode switch
            {
                "PRODUCT_NOT_FOUND" => "Product not found",
                "CATEGORY_NOT_FOUND" => "The specified category does not exist",
                "PRODUCT_DUPLICATE_NAME" => "A product with that name already exists",
                "PRODUCT_HAS_ORDERS" => "It is not possible to delete a product that is present in the orders",
                "INSUFFICIENT_STOCK" => "Insufficient stock",
                "INVALID_QUANTITY" => "Invalid quantity",
                _ => errorCode
            };
        }
    }

    public class CreateProductDto
    {
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public int CategoryId { get; set; }
    }

    public class UpdateProductDto
    {
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public int CategoryId { get; set; }
    }

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
