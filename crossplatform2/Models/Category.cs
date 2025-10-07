using crossplatform2.Models;
using System.ComponentModel.DataAnnotations;

namespace crossplatform2.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public List<Product> Products { get; set; } = new List<Product>();

        // Бизнес-логика
        public int GetTotalProductsCount() => Products.Count; // аналитика количества 

        public decimal GetTotalProductsValue() => Products.Sum(p => p.Price * p.StockQuantity); // подсчёт суммы
    }
}