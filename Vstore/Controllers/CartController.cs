using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Drawing;
using Vstore.DTO;
using Vstore.Models;

namespace Vstore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartController : ControllerBase
    {
        private readonly AppDBContext _context;

        public CartController(AppDBContext context)
        {
            _context = context;
        }


        [HttpPost("add-product-to-cart/{userId}")]
        public async Task<IActionResult> CreateCartAndAddProduct(string userId, int Product_id, int quantity, [FromForm] AddToCartDTO addToCartDTO)
        {
           
            var user = await _context.Users.FirstOrDefaultAsync(o => o.Id == userId && !o.IsDelete);
            if (user == null)
                return NotFound("User not found");


            var cart = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == userId);
            if (cart == null)
            {
                return NotFound("Cart not found. Please register first.");
            }


            var stock = await _context.Stocks.FirstOrDefaultAsync(s =>
                s.Product_Id == Product_id &&
                s.Color_id == addToCartDTO.colorid &&
                s.Size_ID == addToCartDTO.sizeid);

            if (stock == null || stock.Quantity < quantity)
                return BadRequest("Product not available or insufficient stock");


            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.CartId == cart.CartId && ci.ProductId == Product_id);

            if (cartItem != null)
            {

                cartItem.Quantity += quantity;
            }
            else
            {

                cartItem = new CartItem
                {
                    CartId = cart.CartId,
                    ProductId = Product_id,
                    Quantity = quantity,
                    StockId=stock?.Stock_Id
                    
                };
                _context.CartItems.Add(cartItem);
            }


          //  stock.Quantity -= quantity;


            await _context.SaveChangesAsync();
          


            return Ok(new
            {
                Message = "Cart updated successfully",
                CartId = cart.CartId,
                ProductId = Product_id,
                QuantityAdded = quantity,
                colorid= addToCartDTO.colorid,
                sizeid= addToCartDTO.sizeid,
              //  RemainingStock = stock.Quantity
            });
        }



        [HttpDelete("remove-product")]
        public async Task<IActionResult> RemoveProductFromCart([FromForm] int Product_id, [FromQuery] int cartId)
        {
            var cartItem = await _context.CartItems
                .Include(ci => ci.Stock)
                .FirstOrDefaultAsync(ci => ci.ProductId == Product_id && ci.CartId == cartId);

            if (cartItem == null)
                return NotFound("Product not found in cart");

          
            _context.CartItems.Remove(cartItem);
            await _context.SaveChangesAsync();

            return Ok("Product removed from cart");
        }
        [HttpGet("GetCartItemsForUser/{cartId}")]
        public async Task<IActionResult> GetCartItemsForUser(int cartId)
        {
            var cart = await _context.Carts
                .Where(c => c.CartId == cartId)
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                        .ThenInclude(p => p.Images)  
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                        .ThenInclude(p => p.Stock)  
                            .ThenInclude(s => s.color)  
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                        .ThenInclude(p => p.Stock)
                            .ThenInclude(s => s.size)  
                .FirstOrDefaultAsync();

            if (cart == null)
                return NotFound("This user has no cart");


            var cartItemsDto = cart.CartItems.Select(item => new CatItemsDTO
            {

                TotalQuentity = item.Quantity,
                productid = item.ProductId,
                totalprice = item.Quantity * item.Product.Product_Price,
                productname = item.Product.Product_Name,
                

                size = item.Stock.size.Size_Name,
                color = item.Stock.color.Color_Name,
                price = item.Product.Product_Price,
                hasSale = item.Product.Has_Sale,
                priceAfterSelling = item.Product.Product_Price - (item.Product.Product_Price*item.Product.Sale_Percentage/100),

                ImageBase64 = item.Product.DefualtImage != null 
                    ? Convert.ToBase64String(item.Product.DefualtImage)
                    : null,
                Quantity = item.Quantity,
                ShopId= item.Product.Owner_Id
            }).ToList();


            float totalCartPrice = cartItemsDto.Sum(item => item.totalprice);


            int totalCartItems = cartItemsDto.Count;


            var response = new
            {
                TotalItems = totalCartItems,
                TotalPrice = totalCartPrice,
                CartItems = cartItemsDto
            };

            return Ok(response);
        }
        [HttpDelete("remove-all-products/{cartId}")]
        public async Task<IActionResult> RemoveAllProductsFromCart(int cartId)
        {
            var cartItems = await _context.CartItems
                .Include(ci => ci.Stock)
                .Where(ci => ci.CartId == cartId)
                .ToListAsync();

            if (!cartItems.Any())
                return NotFound("No products found in the cart");

            _context.CartItems.RemoveRange(cartItems);
            await _context.SaveChangesAsync();

            return Ok("All products have been removed from the cart.");
        }
        [HttpGet("AvaliabeSizesByColor/{ProductId}")]
        public async Task<IActionResult> GetSizeByColor(int ProductId, int ColorId)
        {
            var sizes = await _context.Stocks
                                              .Where(s => s.Product_Id == ProductId && s.Color_id == ColorId).
                                              Select(s => new { s.Size_ID, s.size.Size_Name })
                                              .ToListAsync();
            return Ok(sizes);
        }

        [HttpGet("AvailableColors/{ProductId}")]
        public async Task<IActionResult> GetAvailableColors(int ProductId)
        {
            var colors = await _context.Stocks
                                        .Where(s => s.Product_Id == ProductId)
                                        .Select(s => new { s.Color_id ,s.color.Color_Name})
                                        .Distinct() 
                                        .ToListAsync();

            return Ok(colors);
        }


    }



    
}
