using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vstore.Services;
using Vstore.DTO;

namespace Vstore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private new List<string> _allowedextension = new List<string> { ".jpg", ".jpeg", ".tif", ".png" };
        private long postersize = 1048567;
        public UserController(UserManager<User> usermanager, IConfiguration configuration, AppDBContext context, NotificationService notificationService)
        {
            _userManager = usermanager;
            this.configuration = configuration;
            _context = context;
            _notificationService = notificationService;
        }
        private readonly NotificationService _notificationService;

        private readonly AppDBContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IConfiguration configuration;
        [HttpGet("GetUserProfile/{userId}")]
        public async Task<IActionResult> GetOwnerDetails(string userId)
        {

            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest("User is required.");
            }

            //var adminEmail = HttpContext.Session.GetString("AdminEmail");
            //if (string.IsNullOrEmpty(adminEmail))
            //{
            //    return BadRequest("Admin email is required.");
            //}


            var user = await _context.Users
                .FirstOrDefaultAsync(o => o.Id == userId && !o.IsDelete);

            if (user == null)
            {
                return NotFound("User not found.");
            }

            //var request = await _context.request
            //    .FirstOrDefaultAsync(r => r. == user.Id);

            var userDetails = new UserProfileDTO
            { id = user.Id,
                FName = user.FName,
                LName = user.LName,
                Email = user.Email,
                UserName = user.UserName,
                Address = user.Address,
                PhoneNumber = user.PhoneNumber,
                ImageBase64 = user.Image != null ? Convert.ToBase64String(user.Image) : null,

            };

            return Ok(userDetails);
        }
        [HttpPut("Update_User/{userId}")]
        public async Task<IActionResult> UpdateUser([FromForm] UpdateUserDTO updateuserDto, string userId)
        {



            var existinguser = await _context.Users
                .FirstOrDefaultAsync(o => o.Id == userId && !o.IsDelete);

            if (existinguser == null)
            {
                return NotFound("User not found.");
            }

            var oldData = new
            {
                existinguser.UserName,
                existinguser.Email,
                existinguser.FName,
                existinguser.LName,
                existinguser.Address,
                existinguser.PhoneNumber,
                existinguser.Image
            };


            existinguser.UserName = updateuserDto.UserName ?? existinguser.UserName;
            existinguser.Email = updateuserDto.Email ?? existinguser.Email;
            existinguser.FName = updateuserDto.FName ?? existinguser.FName;
            existinguser.LName = updateuserDto.LName ?? existinguser.LName;
            existinguser.Address = updateuserDto.Address ?? existinguser.Address;
            existinguser.PhoneNumber = updateuserDto.PhoneNumber ?? existinguser.PhoneNumber;

            if (updateuserDto.Image != null)
            {
                if (!_allowedextension.Contains(Path.GetExtension(updateuserDto.Image.FileName).ToLower()) ||
                    updateuserDto.Image.Length > postersize)
                {
                    return BadRequest("Only tif, jpg, jpeg,png images are allowed, or the file size is too large.");
                }

                using var datastream = new MemoryStream();
                await updateuserDto.Image.CopyToAsync(datastream);
                existinguser.Image = datastream.ToArray();
            }


            await _context.SaveChangesAsync();


            return Ok("user data updated successfully.");
        }
        [HttpPut("Update_photo/{userId}")]
        public async Task<IActionResult> UpdatePhoto([FromForm] UpdatephotoDTO updatephoto, string userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(o => o.Id == userId && !o.IsDelete);

            if (user == null)
            {
                return BadRequest("user not found");
            }
            var oldimage = new
            {
                user.Image
            };
            if (updatephoto.Image != null)
            {
                if (!_allowedextension.Contains(Path.GetExtension(updatephoto.Image.FileName).ToLower()) ||
                    updatephoto.Image.Length > postersize)
                {
                    return BadRequest("Only tif, jpg, jpeg images are allowed, or the file size is too large.");
                }

                using var datastream = new MemoryStream();
                await updatephoto.Image.CopyToAsync(datastream);
                user.Image = datastream.ToArray();
            }


            await _context.SaveChangesAsync();


            return Ok("image updated successfully.");

        }
        [HttpGet("AllShops")]
        public async Task<ActionResult<IEnumerable<ShopDTO>>> GetShops()
        {



            // Project only necessary fields, without including OwnerId
            var Shops = await _context.Owners
               .Where(o => !o.IsDelete&& o.Request.status!=Status.Rejected&& o.Request.status != Status.Pending)
     .Select(o => new ShopDTO
     {
        
         Shop_Name = o.Shop_Name,
         ImageBase64 = o.Image != null ? Convert.ToBase64String(o.Image) : null,
         Shop_Id=o.Id,


     })
     .ToListAsync();


            if (Shops == null || Shops.Count == 0)
            {
                return NotFound(new { message = "No owners found." });
            }

            return Ok(Shops);
        }
        [HttpGet("Get_All_Product/{ownerId}")]
        public async Task<IActionResult> Getproduct(string ownerId)
        {
            var products = await _context.Products
            .Where(o => o.Owner_Id == ownerId && !o.IsDelete)
            .Select(o => new ProductsDTO
            {
                Id=o.Product_Id,
                ProductName = o.Product_Name,
                Product_Price = o.Product_Price,
                Sale_Percentage = o.Sale_Percentage,
                Material = o.Material,
                Product_View = o.Product_View,
                Photo = o.DefualtImage != null ? Convert.ToBase64String(o.DefualtImage) : null// Convert photos to Base64 strings
                                                                                       // OwnerId = o.Owner_Id 
            })
            .ToListAsync();

            if (products == null || products.Count == 0)
            {
                return NotFound("No product found ");
            }

            return Ok(products);
        }
        [HttpGet("Search_Products")]
        public async Task<IActionResult> SearchProducts(
      [FromQuery] string? productName = null,
      [FromQuery] Categories? category = null,
      [FromQuery] string? material = null)
        {
            var query = _context.Products.Where(p => !p.IsDelete);

            if (!string.IsNullOrEmpty(productName))
            {
                query = query.Where(p => p.Product_Name != null && p.Product_Name.StartsWith(productName));
            }

            if (category.HasValue)
            {
                query = query.Where(p => p.Category == category.Value);
            }

            if (!string.IsNullOrEmpty(material))
            {
                query = query.Where(p => p.Material != null && p.Material.StartsWith(material));
            }

            var products = await query
                .Select(p => new ProductsDTO
                {
                    ProductName = p.Product_Name,
                    Product_Price = p.Product_Price,
                    Sale_Percentage = p.Sale_Percentage,
                    Material = p.Material,
                    Product_View = p.Product_View
                })
                .ToListAsync();

            if (!products.Any())
            {
                return NotFound("No products found with the specified criteria.");
            }

            return Ok(products);
        }
        [HttpGet("Search_Shop")]
        public async Task<ActionResult> Search_Shop([FromQuery] string? shopSearch = null, [FromQuery] Categories? categorySearch = null)
        {
            var query = _context.Owners
                .Where(o => !o.IsDelete)
                .Include(o => o.Products)
                .AsQueryable();

            if (!string.IsNullOrEmpty(shopSearch))
            {
                query = query.Where(o => o.Shop_Name != null && o.Shop_Name.StartsWith(shopSearch));
            }

            if (categorySearch.HasValue)
            {
                query = query.Where(o => o.Products.Any(p => p.Category == categorySearch.Value));
            }

            query = query.Where(o => o.Request.status== Status.Accepted);

            var results = await query
                .Select(o => new searchshop
                {
                    Shop_Name = o.Shop_Name,
                    ImageBase64 = o.Image != null ? Convert.ToBase64String(o.Image) : null,
                })
                .ToListAsync();

            if (!results.Any())
            {
                return NotFound("No results found.");
            }

            return Ok(results);
        }




        [HttpPatch("ViewProduct/{productid}")]
        public async Task<IActionResult> ViewProduct(int productid)
        {
            

            if (productid <= 0)
            {
                return BadRequest("Invalid Product ID.");
            }

           
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Product_Id == productid);
            if (product == null)
            {
                return NotFound($"No product found with ID: {productid}");
            }

            // Increment the view count
            product.Product_View++;
            _context.Products.Update(product);
            await _context.SaveChangesAsync();

            return Ok($"The Number of Views is now: {product.Product_View}");
        }
        [HttpGet("GetFavListForUsers/{userId}")]
        public async Task<IActionResult> GetFavList(string userId)
        {
            // Validate userId
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest("userId is required.");
            }


            // Fetch favorite shops
            var favShopIds = await _context.FavLists
             .Where(favList => favList.User_Id == userId)
             .SelectMany(favList => favList.FavListShops)
             .Select(favListShop => favListShop.Owner_Id)
             .Distinct()
             .ToListAsync();

            // Fetch the owners' data based on those IDs
            var owners = await _context.Owners
                .Where(owner => favShopIds.Contains(owner.Id))
                .Select(owner => new FavListForUsers
                {
                    OwnerId = owner.Id,
                    Shop_Name = owner.Shop_Name,
                    ImageBase64 = owner.Image != null
                        ? Convert.ToBase64String(owner.Image)
                        : null
                })
                .ToListAsync();

            if (owners == null || owners.Count == 0)
            {
                return NotFound("No favorite shops found for the user.");
            }

            return Ok(owners);

           
            

        }
        [HttpPost("RateProduct/{productid}")]
        public async Task<IActionResult> RateProduct([FromForm] RateProductDTO rate, int productid)
        {

            var product = await _context.Products
                .Include(p => p.Images)
                .Include(p => p.owner)
                .FirstOrDefaultAsync(p => p.Product_Id == productid);
            if (product == null)
            {
                return NotFound("Product not found.");
            }

            // Step 2: Validate the user
            var usename = await _context.Users.FirstOrDefaultAsync(u => u.Id == rate.User_Id);
            if (usename == null)
            {
                return NotFound("User not found.");
            }

            // Step 3: Check if the user already rated the product
            var existrate = await _context.Rates
                .FirstOrDefaultAsync(r => r.Product_Id == productid && r.User_Id == rate.User_Id);

            if (existrate != null)
            {
                existrate.Rating = rate.Rating;
                _context.Rates.Update(existrate);
            }
            else
            {
                var newrate = new Rate
                {
                    Product_Id = productid,
                    User_Id = rate.User_Id,
                    Rating = rate.Rating,
                };
                await _context.Rates.AddAsync(newrate);
            }

            // Step 4: Create notifications


            var userphoto = usename.Image;

            var notifications = new Notification
            {
                Title = "New Rate!",
                Body = $"The product '{product.Product_Name}' has a new rating from user {usename.FName} {usename.LName}: {rate.Rating}.",
                DateTime = DateTime.Now,
                isread = false,
                User_Id = product.owner.Id,
                Notification_Message = $"The product '{product.Product_Name}' has a new rating from user {usename.FName} {usename.LName}: {rate.Rating}.",
                Image = userphoto
            };

            await _context.Notifications.AddRangeAsync(notifications);

            // Step 5: Save all changes
            await _context.SaveChangesAsync();

            // Step 6: Send SignalR notifications


            var imageBase64 = notifications.Image != null ? Convert.ToBase64String(notifications.Image) : null;
            await _notificationService.SendNotificationWithImage(
                notifications.User_Id,
                notifications.Title,
                notifications.Body,
                imageBase64
            );


            return Ok("User added rating successfully.");
        }
        [HttpGet("GetProductRating/{productId}")]
        public async Task<IActionResult> GetProductRating(int productId)
        {
            var ratings = await _context.Rates
             .Where(r => r.Product_Id == productId)
             .ToListAsync();

            
            if (ratings.Count == 0)
            {
                return Ok("this Product has no Rates yet");
            }

            // Calculate the average rating
            var averageRating = ratings.Average(r => r.Rating);
            var averageRatingmeassage = $"Rating is:{averageRating}";

            return Ok(averageRatingmeassage);
        }




    }

}
