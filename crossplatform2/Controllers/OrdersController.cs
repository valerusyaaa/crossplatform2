using crossplatform2.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace crossplatform2.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class OrdersController : ControllerBase
    {
        private readonly OrderService _orderService;

        public OrdersController(OrderService orderService)
        {
            _orderService = orderService;
        }

        // GET: api/Orders
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Order>>> GetOrders()
        {
            var orders = await _orderService.GetOrdersAsync();
            return Ok(orders);
        }

        // GET: api/Orders/Archived
        [HttpGet("Archived")]
        public async Task<ActionResult<IEnumerable<Order>>> GetArchivedOrders()
        {
            var orders = await _orderService.GetArchivedOrdersAsync();
            return Ok(orders);
        }

        // GET: api/Orders/Cancelled  
        [HttpGet("Cancelled")]
        public async Task<ActionResult<IEnumerable<Order>>> GetCancelledOrders()
        {
            var orders = await _orderService.GetCancelledOrdersAsync();
            return Ok(orders);
        }

        // GET: api/Orders/Completed
        [HttpGet("Completed")]
        public async Task<ActionResult<IEnumerable<Order>>> GetCompletedOrders()
        {
            var orders = await _orderService.GetCompletedOrdersAsync();
            return Ok(orders);
        }

        // GET: api/Orders/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Order>> GetOrder(int id)
        {
            var order = await _orderService.GetOrderByIdAsync(id);
            if (order == null)
            {
                return NotFound();
            }
            return order;
        }

        // POST: api/Orders/CreateOrder
        [HttpPost("CreateOrder")]
        public async Task<ActionResult<Order>> CreateOrder([FromBody] CreateOrderRequest request)
        {
            var (success, order, error) = await _orderService.CreateOrder(request);
            if (!success)
                return BadRequest(new { error = error });

            return CreatedAtAction("GetOrder", new { id = order.Id }, order);
        }

        // POST: api/Orders/1/Complete
        [HttpPost("{id}/Complete")]
        public async Task<ActionResult> CompleteOrder(int id)
        {
            var (success, error) = await _orderService.CompleteOrder(id);
            if (!success)
                return BadRequest(new { error = error });

            return Ok(new { message = "Order is succesfully completed", orderId = id });
        }

        // POST: api/Orders/1/Cancel
        [HttpPost("{id}/Cancel")]
        public async Task<ActionResult> CancelOrder(int id)
        {
            var (success, error) = await _orderService.CancelOrder(id);
            if (!success)
                return BadRequest(new { error = error });

            return Ok(new { message = "The order has been cancelled", orderId = id });
        }

        // POST: api/Orders/1/Archive
        [HttpPost("{id}/Archive")]
        public async Task<ActionResult> ArchiveOrder(int id)
        {
            var (success, error) = await _orderService.ArchiveOrder(id);
            if (!success)
                return BadRequest(new { error = error });

            return Ok(new { message = "The order is archived", orderId = id });
        }

        // GET: api/Orders/Summary
        [HttpGet("Summary")]
        public async Task<ActionResult<object>> GetOrdersSummary()
        {
            var summary = await _orderService.GetOrdersSummaryAsync();
            return Ok(summary);
        }

        // GET: api/Orders/TopProducts?count=3
        [HttpGet("TopProducts")]
        public async Task<ActionResult<IEnumerable<object>>> GetTopProducts(int count = 3)
        {
            var topProducts = await _orderService.GetTopProductsAsync(count);
            return Ok(topProducts);
        }

        // GET: api/Orders/Recent
        [HttpGet("Recent")]
        public async Task<ActionResult<IEnumerable<object>>> GetRecentOrders()
        {
            var recentOrders = await _orderService.GetRecentOrdersAsync();
            return Ok(recentOrders);
        }

        // PUT: api/Orders/5
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateOrder(int id, [FromBody] UpdateOrderRequest request)
        {
            var (success, error) = await _orderService.UpdateOrderAsync(id, request.CustomerName);
            if (!success)
                return BadRequest(error);

            var updatedOrder = await _orderService.GetOrderByIdAsync(id);
            return Ok(updatedOrder);
        }

        // DELETE: api/Orders/5
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteArchivedOrder(int id)
        {
            var (success, error) = await _orderService.DeleteArchivedOrderAsync(id);
            if (!success)
                return BadRequest(error);

            return NoContent();
        }

        // POST: api/Orders/{id}/restore
        [HttpPost("{id}/restore")]
        public async Task<ActionResult> RestoreOrder(int id)
        {
            var (success, error) = await _orderService.RestoreOrderAsync(id);
            if (!success)
                return BadRequest(error);

            return Ok();
        }
    }
}

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

public class UpdateOrderRequest
{
    public string CustomerName { get; set; } = string.Empty;
}