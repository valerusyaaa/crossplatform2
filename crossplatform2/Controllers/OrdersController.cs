using crossplatform2.Data;
using crossplatform2.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel;

namespace crossplatform2.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public OrdersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Orders (только активные)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Order>>> GetOrders()
        {
            return await _context.Orders
                .Where(o => o.Status == OrderStatus.Active)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        // GET: api/Orders/Archived
        [HttpGet("Archived")]
        public async Task<ActionResult<IEnumerable<Order>>> GetArchivedOrders()
        {
            return await _context.Orders
                .Where(o => o.Status == OrderStatus.Archived)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .OrderByDescending(o => o.ArchivedDate)
                .ToListAsync();
        }

        // GET: api/Orders/Cancelled  
        [HttpGet("Cancelled")]
        public async Task<ActionResult<IEnumerable<Order>>> GetCancelledOrders()
        {
            return await _context.Orders
                .Where(o => o.Status == OrderStatus.Cancelled)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        // GET: api/Orders/Completed
        [HttpGet("Completed")]
        public async Task<ActionResult<IEnumerable<Order>>> GetCompletedOrders()
        {
            return await _context.Orders
                .Where(o => o.Status == OrderStatus.Completed)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        // GET: api/Orders/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Order>> GetOrder(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            return order;
        }

        // POST: api/Orders/CreateOrder
        [HttpPost("CreateOrder")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Order>> CreateOrder([FromBody] CreateOrderRequest request) // DTO здесь
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Сервер контролирует создание Order
                var order = new Order
                {
                    CustomerName = request.CustomerName,
                    OrderDate = DateTime.UtcNow,
                    Status = OrderStatus.Active
                };

                foreach (var item in request.OrderItems)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product == null)
                    {
                        return BadRequest($"Product with id {item.ProductId} not found");
                    }

                    order.AddOrderItem(product, item.Quantity);
                }

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                await _context.Entry(order)
                    .Collection(o => o.OrderItems)
                    .Query()
                    .Include(oi => oi.Product)
                    .LoadAsync();

                return CreatedAtAction("GetOrder", new { id = order.Id }, order);
            }
            catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
            {
                await transaction.RollbackAsync();
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return BadRequest(new { error = $"Order creation failed: {ex.Message}" });
            }
        }

        // POST: api/Orders/1/Complete
        [HttpPost("{id}/Complete")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> CompleteOrder(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            order.Complete();
            await _context.SaveChangesAsync();

            return Ok(new { message = "Order completed successfully", orderId = id });
        }

        // POST: api/Orders/1/Cancel
        [HttpPost("{id}/Cancel")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> CancelOrder(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null)
                {
                    return NotFound();
                }

                // Возвращаем товары на склад
                foreach (var item in order.OrderItems)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product != null)
                    {
                        product.StockQuantity += item.Quantity;
                    }
                }

                order.Cancel();
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    message = "Order cancelled and stock restored",
                    orderId = id,
                    details = "All items returned to inventory"
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return BadRequest(new { error = $"Failed to cancel order: {ex.Message}" });
            }
        }

        // POST: api/Orders/1/Archive
        [HttpPost("{id}/Archive")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> ArchiveOrder(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            order.Archive();
            await _context.SaveChangesAsync();

            return Ok(new { message = "Order archived successfully", orderId = id });
        }

        // GET: api/Orders/Summary 
        [HttpGet("Summary")]
        public async Task<ActionResult<object>> GetOrdersSummary()
        {
            var activeOrders = _context.Orders
                .Where(o => o.Status == OrderStatus.Active);
            var completedOrders = _context.Orders
                .Where(o => o.Status == OrderStatus.Completed);

            var completeOrdersCount = await completedOrders.CountAsync();
            var totalActiveOrders = await activeOrders.CountAsync();
            var totalRevenue = await completedOrders.SumAsync(o => o.TotalAmount);
            var averageOrderValue = completeOrdersCount > 0 ? totalRevenue / completeOrdersCount : 0;

            // Добавляем статистику по отмененным заказам
            var cancelledOrdersCount = await _context.Orders
                .Where(o => o.Status == OrderStatus.Cancelled)
                .CountAsync();

            var totalAllOrders = totalActiveOrders + completeOrdersCount + cancelledOrdersCount;
            var cancellationRate = totalAllOrders > 0 ?
                (double)cancelledOrdersCount / totalAllOrders * 100 : 0;

            return new
            {
                CompletedOrders = completeOrdersCount,
                TotalActiveOrders = totalActiveOrders,
                TotalRevenue = totalRevenue,
                AverageOrderValue = averageOrderValue,
                CancelledOrdersCount = cancelledOrdersCount,
                CancellationRate = Math.Round(cancellationRate, 2) 
            };
        }

        // GET: api/Orders/TopProducts?count=5
        [HttpGet("TopProducts")]
        public async Task<ActionResult<IEnumerable<object>>> GetTopProducts(int count = 5)
        {
            var topProductsQuery = _context.OrderItems
                .Include(oi => oi.Product)
                .Include(oi => oi.Order)
                .Where(oi => oi.Order.Status == OrderStatus.Completed)
                .GroupBy(oi => oi.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    ProductName = g.First().Product.Name,
                    TotalQuantity = g.Sum(oi => oi.Quantity),
                    TotalRevenue = g.Sum(oi => oi.Quantity * oi.UnitPrice)
                });

            var topProducts = await topProductsQuery.ToListAsync();
            var sortedTopProducts = topProducts
                .OrderByDescending(x => x.TotalRevenue)
                .Take(count)
                .ToList();

            return Ok(sortedTopProducts);
        }

        // GET: api/Orders/ByCategory
        [HttpGet("ByCategory")]
        public async Task<ActionResult<IEnumerable<object>>> GetOrdersByCategory()
        {
            var ordersByCategoryQuery = _context.OrderItems
                .Include(oi => oi.Product)
                .ThenInclude(p => p.Category)
                .Include(oi => oi.Order)
                .Where(oi => oi.Order.Status == OrderStatus.Active || oi.Order.Status == OrderStatus.Completed)
                .GroupBy(oi => oi.Product.Category.Name)
                .Select(g => new
                {
                    Category = g.Key,
                    TotalQuantity = g.Sum(oi => oi.Quantity),
                    TotalRevenue = g.Sum(oi => oi.Quantity * oi.UnitPrice)
                });

            var ordersByCategory = await ordersByCategoryQuery.ToListAsync();
            var sortedOrdersByCategory = ordersByCategory
                .OrderByDescending(x => x.TotalRevenue)
                .ToList();

            return Ok(sortedOrdersByCategory);
        }

        // GET: api/Orders/Recent
        [HttpGet("Recent")]
        public async Task<ActionResult<IEnumerable<object>>> GetRecentOrders()
        {
            var recentOrders = await _context.Orders
                .Where(o => (o.Status == OrderStatus.Active || o.Status == OrderStatus.Completed || o.Status == OrderStatus.Cancelled) &&
                           o.OrderDate >= DateTime.UtcNow.AddDays(-7))
                .Select(o => new
                {
                    o.Id,
                    o.CustomerName,
                    o.OrderDate,
                    o.TotalAmount,
                    o.Status,
                    ItemCount = o.OrderItems.Count
                })
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return Ok(recentOrders);
        }

        // GET: api/Orders/CustomerStats
        [HttpGet("CustomerStats")]
        public async Task<ActionResult<IEnumerable<object>>> GetCustomerStatistics()
        {
            var customerStatsQuery = _context.Orders
                .Where(o => o.Status == OrderStatus.Active || o.Status == OrderStatus.Completed)
                .GroupBy(o => o.CustomerName)
                .Select(g => new
                {
                    CustomerName = g.Key,
                    TotalOrders = g.Count(),
                    TotalSpent = g.Sum(o => o.TotalAmount),
                    LastOrderDate = g.Max(o => o.OrderDate)
                });

            var customerStats = await customerStatsQuery.ToListAsync();
            var sortedCustomerStats = customerStats
                .OrderByDescending(x => x.TotalSpent)
                .ToList();

            return Ok(sortedCustomerStats);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> UpdateOrder(int id, [FromBody] UpdateOrderRequest request)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();
            if (order.Status != OrderStatus.Active)
                return BadRequest("Only active orders can be updated");

            order.CustomerName = request.CustomerName;
            await _context.SaveChangesAsync();

            return Ok(order);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(
        OperationId = "RemoveArchivedOrder", 
        Summary = "Удалить архивированный заказ",
        Description = "Полностью удаляет архивированный заказ из системы"
            )]
        public async Task<ActionResult> DeleteArchivedOrder(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            if (order.Status != OrderStatus.Archived)
                return BadRequest("Only archived orders can be deleted");

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("{id}/restore")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> RestoreOrder(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            try
            {
                order.RestoreFromCancelled();
                await _context.SaveChangesAsync();
                return Ok(new { message = "Order restored to active", orderId = id });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

    }

    // DTO классы
    public class CreateOrderRequest
    {
        public string CustomerName { get; set; } = string.Empty;
        public List<CreateOrderItem> OrderItems { get; set; } = new List<CreateOrderItem>();
    }

    public class CreateOrderItem
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    // DTO для обновления заказа
    public class UpdateOrderRequest
    {
        public string CustomerName { get; set; } = string.Empty;
    }
}