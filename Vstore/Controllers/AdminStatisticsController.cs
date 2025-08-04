using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Vstore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminStatisticsController : ControllerBase
    {
        public AdminStatisticsController(UserManager<User> usermanager, IConfiguration configuration, AppDBContext context)
        {
            _userManager = usermanager;
            this.configuration = configuration;
            _context = context;
        }

        private readonly AppDBContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IConfiguration configuration;

        [HttpGet("owner-monthly-count")]
        public IActionResult GetOwnerMonthlyRegistrationStats()
        {
            var firstOwner = _context.Owners.OrderBy(o => o.RegistirationDate).FirstOrDefault();
            if (firstOwner == null)
            {
                return Ok(new { message = "No owners found." });
            }

            var startDate = firstOwner.RegistirationDate;
            var endDate = DateTime.UtcNow;

            var monthlyStats = _context.Owners
                .Where(o => o.RegistirationDate >= startDate && o.RegistirationDate <= endDate)
                .GroupBy(o => new { o.RegistirationDate.Year, o.RegistirationDate.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    TotalOwners = g.Count()
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToList();

            return Ok(monthlyStats);
        }
        [HttpGet("AcceptedOwnersPerMonth")]
        public IActionResult GetAcceptedOwnersPerMonth()
        {
            var acceptedOwnersPerMonth = _context.Owners
                .Where(o => o.RegistirationDate != null && o.IsDelete == false&& o.Request.status!=Status.Rejected&&o.Request.status!=Status.Pending)
                .GroupBy(o => new { o.RegistirationDate.Year, o.RegistirationDate.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Count = g.Count()
                })
                .OrderBy(g => g.Year)
                .ThenBy(g => g.Month)
                .ToList();

            return Ok(acceptedOwnersPerMonth);
        }
        [HttpGet("user-monthly-count")]
        public IActionResult GetUserMonthlyRegistrationStats()
        {
            var firstUser = _context.Users.OrderBy(u => u.RegistirationDate).FirstOrDefault();
            if (firstUser == null)
            {
                return Ok(new { message = "No users found." });
            }

            var startDate = firstUser.RegistirationDate;
            var endDate = DateTime.UtcNow;

            // Exclude users who are also owners
            var ownerIds = _context.Owners.Select(o => o.Id).ToList();

            var monthlyStats = _context.Users
                .Where(u => !ownerIds.Contains(u.Id) && u.RegistirationDate >= startDate && u.RegistirationDate <= endDate)
                .GroupBy(u => new { u.RegistirationDate.Year, u.RegistirationDate.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    TotalUsers = g.Count()
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToList();

            return Ok(monthlyStats);
        }
       
        [HttpGet("ShopOwnerStatistics")]
        public IActionResult GetShopOwnerStatistics()
        {
            var totalOwners = _context.Owners.Count();
            var pendingRequests = _context.Requests.Count(r => r.status == Status.Pending);
            var acceptedRequests = _context.Requests.Count(r => r.status == Status.Accepted && r.Owner.IsDelete == false);
            var rejectedRequests = _context.Requests.Count(r => r.status == Status.Rejected);

            return Ok(new { t = "TotalOwners: ", totalOwners, p = "pendingRequests: ", pendingRequests, A = "acceptedRequests: ", acceptedRequests, r = "rejectedRequests: ", rejectedRequests });
        }
        [HttpGet("Registrations-count")]
        public IActionResult GetOwnerCountByDateRange()
        {


            var ownerCount = _context.Owners.Count();


            var AllCount = _context.Users.Count();

            var usercount = AllCount - ownerCount;
            return Ok(new
            {
                TotalOwners = ownerCount,
                TotalUser = usercount
            });
        }
            [HttpGet("ProductStatistics/{OwnerId}")]
        public IActionResult GetProductStatistics(string OwnerId)
        {
            var request = _context.Requests.FirstOrDefault(p => p.OwnerId == OwnerId && p.status == Status.Accepted);

            if (request != null)
            {
                var totalProducts = _context.Products.Count(p => p.Owner_Id == OwnerId);
                var productsOnSale = _context.Products.Count(p => p.Owner_Id == OwnerId && p.Has_Sale);
                var mostViewedProduct = _context.Products
                    .Where(p => p.Owner_Id == OwnerId)
                    .OrderByDescending(p => p.Product_View)
                    .FirstOrDefault();

                return Ok(new
                {
                    t = "totalProducts",
                    totalProducts,
                    onsale = "productsOnSale",
                    productsOnSale,
                    mostview = "mostViewedProduct",
                    mostViewedProduct = mostViewedProduct?.Product_Name ?? "No products available"
                });
            }

            return BadRequest(new { message = "Owner's request is not accepted or does not exist." });
        }
        [HttpGet("TotalIncome")]
        public IActionResult GetTotalIncome()
        {
            var totalIncome = _context.Payments

                .Sum(p => p.Amount);

            return Ok(new
            {

                TotalIncome = totalIncome
            });
        }
        [HttpGet("favlistusercount")]
        public async Task<IActionResult> GetFavListUserCount()
        {
            var countOwnerFavList = await _context.Owners
        .Select(owner => new
        {
            OwnerId = owner.Id,
            OwnerName = owner.UserName,
            UserCount = _context.FavListShops
                .Where(f => f.Owner_Id == owner.Id)
                .Select(f => f.FavList.User_Id)
                .Distinct()
                .Count()
        })
        .OrderByDescending(o => o.UserCount)
        .ToListAsync();

            return Ok(countOwnerFavList);

        }
        [HttpGet("owners-orders")]
        public async Task<IActionResult> GetOwnersOrderCount()
        {

            var ownersOrderCount = await _context.Owners
        .Select(owner => new
        {
            OwnerId = owner.Id,
            OwnerName = owner.UserName,
            OrderCount = _context.Order_Products
                .Where(op => owner.Products.Any(p => p.Product_Id == op.Product_Id))
                .Select(op => op.Order_Id)
                .Distinct()
                .Count()
        })
        .ToListAsync();
            var sortedlist = ownersOrderCount.OrderByDescending(o => o.OrderCount).ToList();

            return Ok(sortedlist);
        }

        [HttpGet("best-selling-shop")]
        public async Task<IActionResult> GetBestSellingOwner()
        {
            var bestSellingOwner = await _context.Order_Products
                .Include(op => op.Product)
                .ThenInclude(p => p.owner)
                .Where(op => op.Order.Payment.Status== "success")
                .GroupBy(op => op.Product.Owner_Id)
                .Select(group => new
                {
                    OwnerId = group.Key,
                    TotalQuantitySold = group.Sum(op => op.Quantity),
                    OwnerName = group.First().Product.owner.Shop_Name,
                    TotalRevenue = group.Sum(op => op.Quantity * op.Product.Product_Price)
                })
                .OrderByDescending(o => o.TotalQuantitySold)
                .FirstOrDefaultAsync();

            if (bestSellingOwner == null)
                return NotFound("No sales data available");

            return Ok(bestSellingOwner);
        }
        [HttpGet("best-selling-category")]
        public async Task<IActionResult> GetBestSellingCategory()
        {
            var bestSellingCategory = await _context.Order_Products
                .Include(op => op.Product)
                .Include(op => op.Order)
                    .ThenInclude(o => o.Payment)
                .Where(op => op.Order != null && op.Order.Payment.Status == "success")
                .GroupBy(op => op.Product.Category)
                .Select(group => new
                {
                    Category = group.Key.ToString(),  // Get the enum name as string
                    TotalQuantitySold = group.Sum(op => op.Quantity),
                    TotalRevenue = group.Sum(op => op.Quantity * op.Product.Product_Price)
                })
                .OrderByDescending(c => c.TotalQuantitySold)
                .FirstOrDefaultAsync();

            if (bestSellingCategory == null)
            {
                return NotFound("No sales data available");
            }

            return Ok(bestSellingCategory);
        }

    }
}

