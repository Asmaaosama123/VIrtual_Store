using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Security.Claims;
using Vstore.Models;
using Vstore.Services;

namespace Vstore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        public ProductController(UserManager<User> usermanager, IConfiguration configuration, AppDBContext context, NotificationService notificationService)
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
        [HttpPost("AddProduct/{ownerId}")]
        public async Task<IActionResult> AddProduct([FromForm] ProductDTO product, string ownerId)
        {
            
            if (!_allowedextension.Contains(Path.GetExtension(product.Photo.FileName).ToLower()) || product.Photo.Length > postersize)
                return BadRequest("Only .tif, .jpg, .jpeg, png images are allowed, or file size exceeds the limit.");
            using var dataStream = new MemoryStream();
            await product.Photo.CopyToAsync(dataStream);
            var products = new Product
            {
                Product_Name = product.ProductName,
                Product_Price = product.Product_Price,
                Has_Sale = product.Has_Sale,
                Sale_Percentage = product.Sale_Percentage,
                Material = product.Material,
                Product_Description = product.Product_Description,
                ProductType = product.Product_Type,
                // Assuming you fetch Categories properly based on business logic:
                Category = product.Categories, // Adjust this line based on how categories are handled
                Category_Id = 1,
                Owner_Id = ownerId,
                DefualtImage = dataStream.ToArray()
            };
           
            await _context.Products.AddAsync(products);
            await _context.SaveChangesAsync();
            var newImage = new Models.Image
            {
                Product_Id = products.Product_Id,
                Photo = products.DefualtImage,
            };
           await _context.Images.AddAsync(newImage);
          
           // _context.SaveChanges();

            await _context.SaveChangesAsync();
            var owner = await _context.Owners.FirstOrDefaultAsync(u => u.Id == ownerId);
            if (owner != null)
            {
                var usersToNotify = await _context.FavListShops
                                                  .Where(fls => fls.Owner_Id == owner.Id)
                                                  .Select(fls => fls.FavList.User_Id)
                                                  .ToListAsync();

                var shopPhoto = owner.Image;
                var saleMessage = "";
                bool issale = false;
                if (products.Has_Sale == true && products.Sale_Percentage > 0)
                {
                    {
                        issale = true;
                    }
                    // Assuming owner's photo is stored in byte[]
                    saleMessage = issale ? $", With Sale {products.Sale_Percentage}%" : "";
                }
                foreach (var userId in usersToNotify)
                {

                    // Create a notification record
                    var notification = new Notification
                    {
                        Title = "New Product Added",
                        Body = $"A new product '{products.Product_Name}' has been added by {owner.Shop_Name}. {saleMessage}",
                        DateTime = DateTime.Now,
                        isread = false,
                        User_Id = userId,
                        Notification_Message = $"Owner added the product '{products.Product_Name}'.",
                        Image = shopPhoto
                    };

                    await _context.Notifications.AddAsync(notification);
                    await _context.SaveChangesAsync(); // Save notifications to the database

                    // Send notification via SignalR or push notification service
                    var imageBase64 = shopPhoto != null ? Convert.ToBase64String(shopPhoto) : null;
                    await _notificationService.SendNotificationWithImage(
                        userId,
                        notification.Title,
                        notification.Body,
                        imageBase64
                    );
                }


            }
            return Ok("Product Added Successfully");
        }



        [HttpGet("Get_Product_Details/{productId}")]
        public async Task<IActionResult> GetProductDetails(int productId)
        {
            // Fetch the product based on productId
            var product = await _context.Products
      .Where(o => o.Product_Id == productId && !o.IsDelete)
      .Select(o => new ProductDetailsDTO
      {
          ProductName = o.Product_Name,
          Product_Price = o.Product_Price,
          Sale_Percentage = o.Sale_Percentage,
          Material = o.Material,
          category = o.Category,  // Enum Category
          Type = o.ProductType,
          Description = o.Product_Description,
          ShopId = o.Owner_Id,
          Product_View = o.Product_View,
          Product_Price_after_sale = o.Sale_Percentage > 0
              ? o.Product_Price - (o.Product_Price * (o.Sale_Percentage / 100.0f))
              : o.Product_Price,
          defualtimage = o.DefualtImage != null ? Convert.ToBase64String(o.DefualtImage) : null,
          Photos = o.Images.Select(p => new PhotoDTO
          {
              ImageId = p.Image_Id,
              Base64Photo = p.Photo != null ? Convert.ToBase64String(p.Photo) : null
             
          }).ToList()
        
      })
      .FirstOrDefaultAsync();



            if (product == null)
            {
                return NotFound("Product not found.");
            }

            return Ok(product);
        }
        [HttpGet("Get_All_Product/{ownerId}")]
        public async Task<IActionResult> Getproduct(string ownerId)
        {
            //var userRole = HttpContext.Session.GetString("UserRole");
            //Console.WriteLine(userRole);

            //if (string.IsNullOrEmpty(userRole) || userRole != "Owner")
            //{
            //    return Forbid("Only owners can add colors."); // 403 Forbidden
            //}

            // Retrieve Owner_Id from session
            //  var ownerId = HttpContext.Session.GetString("OwnerId");

            //if (string.IsNullOrEmpty(ownerId))
            //{
            //    return BadRequest("Owner session is not found or has expired.");
            //}


            var products = await _context.Products
                .Where(o => o.Owner_Id == ownerId && !o.IsDelete)
                .Select(o => new AllProductDTO
                {
                    product_Id = o.Product_Id,
                    ProductName = o.Product_Name,
                    Product_Price = o.Product_Price,
                    Sale_Percentage = o.Sale_Percentage,
                    Material = o.Material,

                    Product_View = o.Product_View,
                    Photo = o.DefualtImage != null ? Convert.ToBase64String(o.DefualtImage) : null,
                    Product_Price_after_sale = o.Sale_Percentage > 0
                        ? o.Product_Price - (o.Product_Price * (o.Sale_Percentage / 100.0f))
                        : o.Product_Price,
                    // OwnerId = o.Owner_Id 
                })
                .ToListAsync();

            if (products == null || products.Count == 0)
            {
                return NotFound("No product found ");
            }

            return Ok(products);


        }
        [HttpPut("UpdateProductData/{productid}")]
        public async Task<IActionResult> UpdateProductData(int productid, [FromForm] UpdateProductDTO updateProduct)
        {
            if (productid <= 0)
                return BadRequest("Product ID is required.");

            if (updateProduct == null)
                return BadRequest("Invalid product data.");

            // Retrieve the product
            var product = await _context.Products.Include(p => p.owner)
                                                 .FirstOrDefaultAsync(p => p.Product_Id == productid);

            if (product == null)
                return NotFound($"No product found with ID: {productid}");

            // Store old values before updating


            // Update product properties if provided
            if (!string.IsNullOrEmpty(updateProduct.ProductName))
                product.Product_Name = updateProduct.ProductName;

            if (updateProduct.Product_Price > 0)
                product.Product_Price = updateProduct.Product_Price;

            if (!string.IsNullOrEmpty(updateProduct.Material))
                product.Material = updateProduct.Material;

            product.Has_Sale = updateProduct.Has_Sale;

            if (updateProduct.Sale_Percentage >= 0) // Ensure valid percentage
                product.Sale_Percentage = updateProduct.Sale_Percentage;
            if (!string.IsNullOrEmpty(updateProduct.Product_Type))
                product.ProductType = updateProduct.Product_Type;

            // Handle category update with a list of categories
            var categoryList = Enum.GetValues(typeof(Categories))
                                   .Cast<Categories>()
                                   .Select(c => new { Id = (int)c, Name = c.ToString() })
                                   .ToList();

            if (updateProduct.Categories != null && Enum.IsDefined(typeof(Categories), updateProduct.Categories))
            {
                product.Category = updateProduct.Categories;
                product.Category_Id = 1;
            }
            else
            {
                return BadRequest("Invalid category provided.");
            }

            // Handle product image update
            //if (updateProduct.Photo != null)
            //{
            //    var fileExtension = Path.GetExtension(updateProduct.Photo.FileName).ToLower();

            //    if (!_allowedextension.Contains(fileExtension) || updateProduct.Photo.Length > 5 * 1024 * 1024) // 5MB limit
            //        return BadRequest("Only tif, jpg, jpeg images are allowed, or the file size is too large.");

            //    using var datastream = new MemoryStream();
            //    await updateProduct.Photo.CopyToAsync(datastream);
            //    product.DefualtImage = datastream.ToArray();
            //}

            // Save changes to database
            _context.Products.Update(product);
            await _context.SaveChangesAsync();

            // Send notifications if product is on sale
            if (updateProduct.Has_Sale && updateProduct.Sale_Percentage > 0 && product.owner != null)
            {
                var usersToNotify = await _context.FavListShops
                                                  .Where(fls => fls.Owner_Id == product.owner.Id)
                                                  .Select(fls => fls.FavList.User_Id)
                                                  .ToListAsync();

                var shopPhoto = product.owner.Image;
                var newPrice = product.Product_Price * ((100.0 - product.Sale_Percentage) / 100.0);

                foreach (var userId in usersToNotify)
                {
                    var notification = new Notification
                    {
                        Title = "Sale Update!",
                        Body = $"The product '{product.Product_Name}' is now on sale by '{product.owner.Shop_Name}' with a {product.Sale_Percentage}% discount. New price: {newPrice} (was {product.Product_Price}).",
                        DateTime = DateTime.Now,
                        isread = false,
                        User_Id = userId,
                        Notification_Message = $"Owner {product.owner.Shop_Name} updated the sale for '{product.Product_Name}'.",
                        Image = shopPhoto
                    };

                    _context.Notifications.Add(notification);
                }

                await _context.SaveChangesAsync();

                // Send notifications via SignalR
                foreach (var userId in usersToNotify)
                {
                    var notification = await _context.Notifications
                        .OrderByDescending(n => n.Notification_Id)
                        .FirstOrDefaultAsync(n => n.User_Id == userId && n.Body.Contains(product.Product_Name));

                    if (notification != null)
                    {
                        var imageBase64 = notification.Image != null ? Convert.ToBase64String(notification.Image) : null;
                        await _notificationService.SendNotificationWithImage(
                            userId,
                            notification.Title,
                            notification.Body,
                            imageBase64
                        );
                    }
                }
            }

            return Ok(new
            {
                Message = "Product updated successfully.",
                UpdatedValues = new
                {
                    product.Product_Id,
                    product.Product_Name,
                    product.Product_Price,
                    product.Material,
                    product.Has_Sale,
                    product.Sale_Percentage,
                    product.Category_Id,
                    product.ProductType
                },
            });
        }



    
          
        [HttpDelete("DeleteProduct/{id}")]
        public async Task<IActionResult> SoftDeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound("Product not found");
            }

            product.IsDelete = true;
            _context.Products.Update(product);
            await _context.SaveChangesAsync();

            return Ok("Product deleted");
        }
    }
    }

