using System.ComponentModel.DataAnnotations;

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

        public void DecreaseStock(int quantity) // Управление запасами с валидацией
        {
            if (quantity <= 0) throw new ArgumentException("Quantity must be positive");
            if (StockQuantity < quantity) throw new InvalidOperationException("Not enough stock");
            StockQuantity -= quantity;
        }

        public void IncreaseStock(int quantity) // Безопасное увеличение запасов
        {
            if (quantity <= 0) throw new ArgumentException("Quantity must be positive");
            StockQuantity += quantity;
        }
    }
}