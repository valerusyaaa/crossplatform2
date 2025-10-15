using crossplatform2.Models;
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

        public void CalculateTotal()
        {
            TotalAmount = OrderItems.Sum(item => item.Quantity * item.UnitPrice);
        }

        public (bool success, string? error) AddOrderItem(Product product, int quantity)
        {
            if (!product.IsAvailable)
                return (false, "PRODUCT_NOT_AVAILABLE");

            var (decreaseSuccess, decreaseError) = product.DecreaseStock(quantity);
            if (!decreaseSuccess)
                return (false, decreaseError);

            var orderItem = new OrderItem
            {
                ProductId = product.Id,
                Quantity = quantity,
                UnitPrice = product.Price
            };

            OrderItems.Add(orderItem);
            CalculateTotal();
            return (true, null);
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

        public enum OrderOperationResult
        {
            Success,
            NotActive,
            NotCancelled,
            NotCompletedOrCancelled
        }

        public OrderOperationResult MoveToCompleted()
        {
            if (Status != OrderStatus.Active)
                return OrderOperationResult.NotActive;

            Status = OrderStatus.Completed;
            CompletedDate = DateTime.UtcNow;
            return OrderOperationResult.Success;
        }

        public OrderOperationResult MoveToCancelled()
        {
            if (Status != OrderStatus.Active)
                return OrderOperationResult.NotActive;

            Status = OrderStatus.Cancelled;
            CancelledDate = DateTime.UtcNow;
            return OrderOperationResult.Success;
        }

        public OrderOperationResult RestoreFromCancelled()
        {
            if (Status != OrderStatus.Cancelled)
                return OrderOperationResult.NotCancelled;

            Status = OrderStatus.Active;
            CancelledDate = null;
            return OrderOperationResult.Success;
        }

        public OrderOperationResult MoveToArchived()
        {
            if (Status != OrderStatus.Completed && Status != OrderStatus.Cancelled)
                return OrderOperationResult.NotCompletedOrCancelled;

            Status = OrderStatus.Archived;
            ArchivedDate = DateTime.UtcNow;
            return OrderOperationResult.Success;
        }
    }
    public enum OrderStatus
    {
        Active = 1,      
        Completed = 2,  
        Cancelled = 3,   
        Archived = 4     
    }
}