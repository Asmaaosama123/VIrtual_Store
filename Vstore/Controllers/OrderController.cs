using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vstore.Data;
using Vstore.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Vstore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly AppDBContext _context;

        public OrderController(AppDBContext context)
        {
            _context = context;
        }

        [HttpPost("create-order/{userId}")]
        public async Task<IActionResult> CreateOrder(string userId, [FromBody] CreateOrderFromCartDTO dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDelete);
            if (user == null)
                return NotFound("User not found");

            var cart = await _context.Carts.Include(c => c.CartItems).FirstOrDefaultAsync(c => c.UserId == userId);
            if (cart == null || !cart.CartItems.Any())
                return BadRequest("Cart is empty");


            var order = new Order
            {
                User_Id = userId,
                Date = DateTime.UtcNow,
                PaymentMethod= dto.PaymentMethod,
               
                Order_Products = cart.CartItems.Select(ci => new Order_Product
                {
                    Product_Id = ci.ProductId,
                    Quantity = ci.Quantity,
                    StockId=(int)ci.StockId
                    

                }).ToList()

            };

            _context.Orders.Add(order);
            _context.CartItems.RemoveRange(cart.CartItems);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Order placed successfully", OrderId = order.Order_Id });
        }
        [HttpGet("payment-methods")]
        public IActionResult GetPaymentMethods()
        {
            var methods = Enum.GetValues(typeof(Way))
                .Cast<Way>()
                .Select(w => new
                {
                    Id = (int)w,
                    Name = w.ToString()
                })
                .ToList();

            return Ok(methods);
        }

        [HttpGet("get-orders/{userId}")]
        public async Task<IActionResult> GetOrders(string userId)
        {
            var orders = await _context.Orders
                .Where(o => o.User_Id == userId)
                .Include(o => o.Order_Products)
                    .ThenInclude(op => op.Product)
                .ToListAsync();

            if (!orders.Any())
                return NotFound("No orders found");

            var orderDtos = orders.Select(o => new OrderDTO
            {
                Order_Id = o.Order_Id,
                User_Id = o.User_Id,
                PaymentMethod= o.PaymentMethod,
                

                TotalPrice = o.Order_Products.Sum(op =>
                {
                    var discount = op.Product.Product_Price * op.Product.Sale_Percentage / 100;
                    var finalPrice = op.Product.Product_Price - discount;
                    return finalPrice * op.Quantity;
                }),

                TotalQuantity = o.Order_Products.Sum(op => op.Quantity),

                Order_Products = o.Order_Products.Select(op => new ProductOrderDTO
                {
                    ProductId = op.Product.Product_Id,
                    ProductName = op.Product.Product_Name,
                    Product_Price = op.Product.Product_Price,
                    PriceAfterSell = op.Product.Product_Price - (op.Product.Product_Price * op.Product.Sale_Percentage / 100),
                    Quntity = op.Quantity,
                    ShopId= op.Product.Owner_Id,
                    Photo = op.Product.DefualtImage != null
                        ? Convert.ToBase64String(op.Product.DefualtImage)
                        : null
                }).ToList()
            }).ToList();

            return Ok(orderDtos);
        }




        [HttpDelete("delete-order/{orderId}")]
        public async Task<IActionResult> DeleteOrder(int orderId)
        {
            var order = await _context.Orders.Include(o => o.Order_Products).FirstOrDefaultAsync(o => o.Order_Id == orderId);
            if (order == null)
                return NotFound("Order not found");

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            return Ok("Order deleted successfully");
        }
    }
}
