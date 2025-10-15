using crossplatform2.Data;
using crossplatform2.Models;
using Microsoft.EntityFrameworkCore;

public class OrderService
{
    private readonly ApplicationDbContext _context;

    public OrderService(ApplicationDbContext context)
    {
        _context = context;
    }
    public async Task<List<Order>> GetOrdersAsync()
    {
        return await _context.Orders
            .Where(o => o.Status == OrderStatus.Active)
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    }

    public async Task<List<Order>> GetArchivedOrdersAsync()
    {
        return await _context.Orders
            .Where(o => o.Status == OrderStatus.Archived)
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .OrderByDescending(o => o.ArchivedDate)
            .ToListAsync();
    }

    public async Task<List<Order>> GetCancelledOrdersAsync()
    {
        return await _context.Orders
            .Where(o => o.Status == OrderStatus.Cancelled)
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    }

    public async Task<List<Order>> GetCompletedOrdersAsync()
    {
        return await _context.Orders
            .Where(o => o.Status == OrderStatus.Completed)
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    }

    public async Task<Order?> GetOrderByIdAsync(int id)
    {
        return await _context.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == id);
    }
    public async Task<object> GetOrdersSummaryAsync()
    {
        var activeOrders = await _context.Orders
            .Where(o => o.Status == OrderStatus.Active)
            .CountAsync();

        var completedOrders = await _context.Orders
            .Where(o => o.Status == OrderStatus.Completed)
            .CountAsync();

        var cancelledOrdersCount = await _context.Orders
            .Where(o => o.Status == OrderStatus.Cancelled)
            .CountAsync();

        var totalRevenue = await _context.Orders.SumAsync(o => o.TotalAmount);
        var averageOrderValue = completedOrders > 0 ? totalRevenue / completedOrders : 0;

        var totalAllOrders = activeOrders + completedOrders + cancelledOrdersCount;
        var cancellationRate = totalAllOrders > 0 ?
            (double)cancelledOrdersCount / totalAllOrders * 100 : 0;
        var completionRate = totalAllOrders > 0 ?
            (double)completedOrders / totalAllOrders * 100 : 0;

        return new
        {
            TotalOrders = totalAllOrders,
            CompletionRate = Math.Round(completionRate, 2),
            TotalRevenue = totalRevenue,
            AverageOrderValue = averageOrderValue,
            CancellationRate = Math.Round(cancellationRate, 2)
        };
    }

    public async Task<List<object>> GetTopProductsAsync(int count = 3)
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
            .ToList<object>();

        return sortedTopProducts;
    }

    public async Task<List<object>> GetRecentOrdersAsync()
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
            .ToListAsync<object>();

        return recentOrders;
    }
    public async Task<(bool success, string? error)> UpdateOrderAsync(int id, string customerName)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            return (false, "Order not found");

        if (order.Status != OrderStatus.Active)
            return (false, "Only active orders can be updated");

        order.CustomerName = customerName;
        await _context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool success, string? error)> DeleteArchivedOrderAsync(int id)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null)
            return (false, "Order not found");

        if (order.Status != OrderStatus.Archived)
            return (false, "Only archived orders can be deleted");

        _context.Orders.Remove(order);
        await _context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool success, string? error)> RestoreOrderAsync(int id)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null)
            return (false, "Order not found");

        order.RestoreFromCancelled();
        await _context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool success, Order? order, string? error)> CreateOrder(CreateOrderRequest request)
    {
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
                return (false, null, $"Product with id {item.ProductId} not found");

            var (itemSuccess, itemError) = order.AddOrderItem(product, item.Quantity);
            if (!itemSuccess)
                return (false, null, itemError);
        }

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // Загружаем связанные данные
        await _context.Entry(order)
            .Collection(o => o.OrderItems)
            .Query()
            .Include(oi => oi.Product)
            .LoadAsync();

        return (true, order, null);
    }

    public async Task<(bool success, string? error)> CompleteOrder(int orderId)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null)
            return (false, "Order was not found");

        if (order.Status != OrderStatus.Active)
            return (false, "You can only complete active orders");

        order.Complete();
        await _context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool success, string? error)> CancelOrder(int orderId)
    {

        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
            return (false, "Order was not found");

        if (order.Status != OrderStatus.Active)
            return (false, "You can only complete active orders");

            //Возвращаем товары на склад
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
        return (true, null);

    }

    public async Task<(bool success, string? error)> ArchiveOrder(int orderId)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null)
            return (false, "Order was not found");

        if (order.Status != OrderStatus.Completed && order.Status != OrderStatus.Cancelled)
            return (false, "You can archive only completed or cancelled orders.");

        order.Archive();
        await _context.SaveChangesAsync();
        return (true, null);
    }
}