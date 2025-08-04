using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Vstore.Models;
using Vstore.Services;

namespace Vstore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OwnerStatisticsController : ControllerBase
    {
        public OwnerStatisticsController(UserManager<User> usermanager, IConfiguration configuration, AppDBContext context, NotificationService notificationService)
        {
            _userManager = usermanager;
            this.configuration = configuration;
            _context = context;
            _notificationService = notificationService;
        }
        private readonly NotificationService _notificationService;
        private new List<string> _allowedextension = new List<string> { ".jpg", ".jpeg", ".tif", ".png" };
        private long postersize = 10048567;
        private readonly AppDBContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IConfiguration configuration;

        [HttpGet("owner-count")]
        public IActionResult GetOwnerCountByDateRange([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate > endDate)
            {
                return BadRequest(new { message = "Start date must be before end date." });
            }

            var ownerCount = _context.Owners
                .Count(o => o.RegistirationDate >= startDate && o.RegistirationDate <= endDate);

            var AllCount = _context.Users
               .Count(o => o.RegistirationDate >= startDate && o.RegistirationDate <= endDate);

            var usercount = AllCount - ownerCount;
            return Ok(new
            {
                StartDate = startDate,
                EndDate = endDate,
                TotalOwners = ownerCount,
                TotalUser = usercount
            });
        }










        [HttpGet("products-views/{OwnerId}")]
        public async Task<IActionResult> GetProductViewCount(string OwnerId)
        {
            var owner = await _context.Owners.Where(o => o.Id == OwnerId).FirstOrDefaultAsync();
            if (owner == null)
                return NotFound("Owner not found.");


            var productViews = await _context.Products
                .Where(p => p.Owner_Id == OwnerId)
                .Select(pro => new
                {
                    ProductId = pro.Product_Id,
                    ProductName = pro.Product_Name,
                    ViewCount = pro.Product_View
                })
                .OrderByDescending(p => p.ViewCount)
                .ToListAsync();

            return Ok(productViews);


        }
       
      
        [HttpGet("owner-products-with-ratings/{ownerId}")]
        public async Task<IActionResult> GetOwnerProductsWithRatings(string ownerId)
        {
            var ownerProducts = await _context.Products
                .Where(p => p.Owner_Id == ownerId && !p.IsDelete)
                .Select(p => new
                {
                    //ProductId = p.Product_Id,
                    ProductName = p.Product_Name,
                    //Price = p.Product_Price,
                    AverageRating = p.Rate.Any() ? p.Rate.Average(r => r.Rating) : 0,
                    TotalRatings = p.Rate.Count(),
                    //ImageBase64 = p != null && p.Images.Any()
                    //    ? Convert.ToBase64String(p.Images.First().Photo)
                    //    : null
                })
                .ToListAsync();

            if (ownerProducts == null || !ownerProducts.Any())
            {
                return NotFound("No products found for this owner");
            }

            return Ok(ownerProducts);
        }
      
      
       

      
      
        [HttpGet("monthly-total-amount/{ownerId}")]
        public async Task<IActionResult> GetMonthlyTotalAmount(string ownerId)
        {
            var payments = await _context.Payments
                .Where(p =>
                    p.Order.Order_Products.Any(op =>
                        op.Product.Owner_Id == ownerId && op.IsDelete == false))
                .ToListAsync();

            var monthlyTotals = payments
                .GroupBy(p => new { p.CreatedAt.Year, p.CreatedAt.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    TotalAmount = g.Sum(p => p.Amount)
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToList();

            return Ok(monthlyTotals);
        }
      
      
      
       
        [HttpGet("orders-per-month/{OwnerId}/{year}")]
        public async Task<IActionResult> GetOrdersPerMonth(string OwnerId, int year)
        {
            var result = await _context.Orders
                .Include(o => o.Order_Products)
                    .ThenInclude(op => op.Product)
                .Where(o => o.Date.Year == year &&
                            o.Order_Products.Any(op => op.Product.Owner_Id == OwnerId)) // <-- filtering by OwnerId
                .GroupBy(o => o.Date.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    OrderCount = g.Count()
                })
                .ToListAsync();

            var fullResult = Enumerable.Range(1, 12)
                .Select(m => new OrdersPerMonthDto
                {
                    Month = m,
                    MonthName = new DateTime(1, m, 1).ToString("MMMM"),
                    OrderCount = result.FirstOrDefault(r => r.Month == m)?.OrderCount ?? 0
                })
                .ToList();

            return Ok(fullResult);
        }






       
        [HttpGet("order-status-count/{ownerId}/{year}/{month}")]
        public async Task<IActionResult> GetOrderStatusCount(string ownerId, int year, int month)
        {
            var orders = await _context.Orders
                .Where(o => o.Date != null && o.Date.Year == year && o.Date.Month == month) // Fix here
                .Include(o => o.Order_Products)
                    .ThenInclude(op => op.Product)
                .Include(o => o.Payment)
                .ToListAsync();

            var filteredOrders = orders
                .Where(o => o.Order_Products.Any(op => op.Product != null && op.Product.Owner_Id == ownerId))
                .GroupBy(o => o.Payment != null ? o.Payment.Status.ToLower() : "not_paid")
                .Select(g => new
                {
                    Status = g.Key == "success" ? "Paid" : "Not Paid",
                    Count = g.Count()
                })
                .ToList();

            var response = new List<object>
    {
        new { Status = "Paid", Count = filteredOrders.FirstOrDefault(x => x.Status == "Paid")?.Count ?? 0 },
        new { Status = "Not Paid", Count = filteredOrders.FirstOrDefault(x => x.Status == "Not Paid")?.Count ?? 0 }
    };

            return Ok(response);
        }
        [HttpGet("revenue-per-month/{year}")]
        public async Task<IActionResult> GetRevenuePerMonth(int year)
        {
            var result = await _context.Payments
                .Where(p => p.Status == "success" && p.CreatedAt.Year == year)
                .GroupBy(p => p.CreatedAt.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    MonthName = new DateTime(1, g.Key, 1).ToString("MMMM"),
                    TotalRevenue = g.Sum(p => p.Amount)
                })
                .ToListAsync();

            // Ensure all 12 months appear in the response (even if revenue = 0)
            var fullResult = Enumerable.Range(1, 12)
                .Select(m => new
                {
                    Month = m,
                    MonthName = new DateTime(1, m, 1).ToString("MMMM"),
                    TotalRevenue = result.FirstOrDefault(r => r.Month == m)?.TotalRevenue ?? 0
                })
                .OrderBy(x => x.Month)
                .ToList();

            return Ok(fullResult);
        }
        [HttpGet("sold-products/{ownerId}")]
        public async Task<IActionResult> GetSoldProductsForShop(string ownerId)
        {
            var soldProducts = await _context.Payments
                .Where(p => p.Status.ToLower() == "success" &&
                            p.Order != null &&
                            p.Order.Order_Products.Any(op => op.Product != null && op.Product.Owner_Id.ToLower() == ownerId.ToLower()))
                .SelectMany(p => p.Order.Order_Products
                    .Where(op => op.Product != null && op.Product.Owner_Id.ToLower() == ownerId.ToLower()))
                .GroupBy(op => new { op.Product.Product_Id, op.Product.Product_Name })
                .Select(g => new
                {
                    ProductId = g.Key.Product_Id,
                    ProductName = g.Key.Product_Name,
                    TotalQuantitySold = g.Sum(op => op.Quantity)
                })
                .OrderByDescending(x => x.TotalQuantitySold)
                .ToListAsync();

            if (!soldProducts.Any())
            {
                return NotFound("No sold products found for this owner.");
            }

            return Ok(soldProducts);
        }

        [HttpGet("sold-main-categories/{ownerId}/{year}/{month}")]
        public async Task<IActionResult> GetSoldPerMainCategory(string ownerId, int year, int month)
        {
            var result = await _context.Payments
                .Where(p => p.Status.ToLower() == "success" && p.CreatedAt != null && p.CreatedAt.Year == year && p.CreatedAt.Month == month)
                .Include(p => p.Order)
                    .ThenInclude(o => o.Order_Products)
                        .ThenInclude(op => op.Product)
                .ToListAsync(); // Fetch data first to avoid deferred execution issues

            var soldProducts = result
                .Where(p => p.Order != null)
                .SelectMany(p => p.Order.Order_Products
                    .Where(op => op.Product != null && op.Product.Owner_Id.ToLower() == ownerId.ToLower()))
                .GroupBy(op => op.Product.Category.ToString()) // Convert category enum to string
                .Select(g => new
                {
                    Category = g.Key,
                    TotalQuantitySold = g.Sum(op => op.Quantity)
                })
                .OrderByDescending(x => x.TotalQuantitySold)
                .ToList();

            // Ensure all categories are present even if 0
            var allCategories = Enum.GetNames(typeof(Categories))
                .Select(c => new
                {
                    Category = c,
                    TotalQuantitySold = soldProducts.FirstOrDefault(r => r.Category == c)?.TotalQuantitySold ?? 0
                })
                .OrderByDescending(x => x.TotalQuantitySold)
                .ToList();

            return Ok(allCategories);
        }
        [HttpGet("monthly-revenue/{ownerId}/{year}")]
        public async Task<IActionResult> GetMonthlyRevenue(string ownerId, int year)
        {
            var payments = await _context.Payments
                .Where(p => p.Status.ToLower() == "success" && p.CreatedAt != null && p.CreatedAt.Year == year)
                .Include(p => p.Order)
                    .ThenInclude(o => o.Order_Products)
                        .ThenInclude(op => op.Product)
                .ToListAsync(); // Fetch first to prevent deferred execution issues

            var revenueData = payments
                .Where(p => p.Order != null)
                .Select(p => new
                {
                    Month = p.CreatedAt.Month,
                    TotalAmount = p.Order.Order_Products
                        .Where(op => op.Product != null && op.Product.Owner_Id.ToLower() == ownerId.ToLower())
                        .Any() ? p.Amount / 100 : 0 // 🔥 Divide amount by 100
                })
                .GroupBy(x => x.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    MonthName = new DateTime(1, g.Key, 1).ToString("MMMM"),
                    TotalAmount = g.Sum(x => x.TotalAmount) // Sum per month correctly
                })
                .ToList();

            // Ensure all 12 months are present
            var fullResult = Enumerable.Range(1, 12)
                .Select(m => new
                {
                    Month = m,
                    MonthName = new DateTime(1, m, 1).ToString("MMMM"),
                    TotalAmount = revenueData.FirstOrDefault(r => r.Month == m)?.TotalAmount ?? 0
                })
                .OrderBy(x => x.Month)
                .ToList();

            return Ok(fullResult);
        }
    }


}
