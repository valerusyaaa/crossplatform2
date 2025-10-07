using crossplatform2.Data;
using System.ComponentModel.DataAnnotations;

namespace crossplatform2.Models
{
    public class Order
    {
        public int Id { get; set; }

        [Required]
        public string CustomerName { get; set; } = string.Empty;

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        public decimal TotalAmount { get; set; }

        public OrderStatus Status { get; set; } = OrderStatus.Active;

        public DateTime? CompletedDate { get; set; }
        public DateTime? CancelledDate { get; set; }
        public DateTime? ArchivedDate { get; set; }

        public List<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

        // Бизнес-логика
        public void CalculateTotal()
        {
            TotalAmount = OrderItems.Sum(item => item.Quantity * item.UnitPrice);
        }

        public void AddOrderItem(Product product, int quantity)
        {
            if (!product.IsAvailable)
                throw new InvalidOperationException("Product is not available");

            if (quantity > product.StockQuantity)
                throw new InvalidOperationException("Not enough stock");

            product.DecreaseStock(quantity);

            var orderItem = new OrderItem
            {
                ProductId = product.Id,
                Quantity = quantity,
                UnitPrice = product.Price
            };

            OrderItems.Add(orderItem);
            CalculateTotal();
        }

        // методы для управления статусом
        public void Complete()
        {
            Status = OrderStatus.Completed;
        }

        public void Cancel()
        {
            Status = OrderStatus.Cancelled;
        }

        public void Archive()
        {
            Status = OrderStatus.Archived;
            ArchivedDate = DateTime.UtcNow;
        }

        public void MoveToCompleted()
        {
            if (Status != OrderStatus.Active)
                throw new InvalidOperationException("Only active orders can be completed");

            Status = OrderStatus.Completed;
            CompletedDate = DateTime.UtcNow;
        }

        public void MoveToCancelled()
        {
            if (Status != OrderStatus.Active)
                throw new InvalidOperationException("Only active orders can be cancelled");

            Status = OrderStatus.Cancelled;
            CancelledDate = DateTime.UtcNow;
        }

        public void RestoreFromCancelled()
        {
            if (Status != OrderStatus.Cancelled)
                throw new InvalidOperationException("Only cancelled orders can be restored");

            Status = OrderStatus.Active;
            CancelledDate = null;
        }

        public void MoveToArchived()
        {
            if (Status != OrderStatus.Completed && Status != OrderStatus.Cancelled)
                throw new InvalidOperationException("Only completed or cancelled orders can be archived");

            Status = OrderStatus.Archived;
            ArchivedDate = DateTime.UtcNow;
        }
    }



    public enum OrderStatus
    {
        Active = 1,      // Активный заказ
        Completed = 2,   // Выполнен
        Cancelled = 3,   // Отменен
        Archived = 4     // В архиве
    }
}