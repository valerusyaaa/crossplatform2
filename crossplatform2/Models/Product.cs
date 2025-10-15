using System.ComponentModel.DataAnnotations;
using static crossplatform2.Controllers.ProductsController;

namespace crossplatform2.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        [Range(0, int.MaxValue)]
        public int StockQuantity { get; set; } // количество запасов

        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        // Бизнес-логика
        public bool IsAvailable => StockQuantity > 0; // Правило доступности

        public (bool success, string error) DecreaseStock(int quantity)
        {
            if (quantity <= 0)
                return (false, "INVALID_QUANTITY");

            if (StockQuantity < quantity)
                return (false, "INSUFFICIENT_STOCK");

            StockQuantity -= quantity;
            return (true, string.Empty);
        }

        public (bool success, string error) IncreaseStock(int quantity)
        {
            if (quantity <= 0)
                return (false, "ERROR");

            StockQuantity += quantity;
            return (true, string.Empty);
        }
    }
}