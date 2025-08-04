using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mail;
using System.Net;
using Vstore.Models;
using Vstore.DTO;
using System.Drawing;
using static System.Net.Mime.MediaTypeNames;
using Vstore.Services;

namespace Vstore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OwnerController : ControllerBase
    {
        private new List<string> _allowedextension = new List<string> { ".jpg", ".jpeg", ".tif", ".png" };
        private long postersize = 1048567;
        public OwnerController(UserManager<User> usermanager, IConfiguration configuration, AppDBContext context, NotificationService notificationService)
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
        private async Task SendEmailAsync(string email, string subject, string message)
        {
            using var client = new SmtpClient(configuration["EmailSettings:SmtpServer"], int.Parse(configuration["EmailSettings:SmtpPort"]))
            {
                Credentials = new NetworkCredential(configuration["EmailSettings:SmtpUser"], configuration["EmailSettings:SmtpPass"]),
                EnableSsl = true
            };


            var mailMessage = new MailMessage
            {
                From = new MailAddress(configuration["EmailSettings:SmtpUser"]),
                Subject = subject,
                Body = message,
                IsBodyHtml = true
            };
            mailMessage.To.Add(email);

            await client.SendMailAsync(mailMessage);
        }
      



        [HttpPost("Owner_Register")]
        public async Task<IActionResult> RegisterOwner([FromForm] NewOwner owner)
        {


            var passwordHasher = new PasswordHasher<Owner>();
            if (owner.Password != owner.ConfirmPassword)
            {
                ModelState.AddModelError("ConfirmPassword", "Passwords do not match.");
                return BadRequest(ModelState);
            }

            if (owner.Image == null || string.IsNullOrEmpty(owner.Image.FileName))
                return BadRequest("Image is required.");

            if (!_allowedextension.Contains(Path.GetExtension(owner.Image.FileName).ToLower()) || owner.Image.Length > postersize)
                return BadRequest("Only .tif, .jpg, .jpeg images are allowed, or file size exceeds the limit.");

            using var datastream = new MemoryStream();
            await owner.Image.CopyToAsync(datastream);

            var deletedOwner = await _context.Owners.Include(o => o.Request)
                .FirstOrDefaultAsync(o => o.Email == owner.Email);

            if (deletedOwner != null)
            {
                if (deletedOwner.IsDelete)
                {
                    deletedOwner.IsDelete = false;
                    deletedOwner.UserName = owner.UserName;
                    deletedOwner.FName = owner.FName;
                    deletedOwner.LName = owner.LName;
                    deletedOwner.PasswordHash = passwordHasher.HashPassword(deletedOwner, owner.Password);
                    deletedOwner.Address = owner.Address;
                    deletedOwner.PhoneNumber = owner.PhoneNumber;
                    deletedOwner.Shop_Name = owner.Shop_Name;
                    deletedOwner.Shop_Description = owner.Shop_Description;
                    deletedOwner.Image = datastream.ToArray();
                    deletedOwner.Roles = Roles.Owner;

                    if (deletedOwner.Request != null && deletedOwner.Request.status == Status.Accepted)
                    {
                        deletedOwner.Request.status = Status.Pending;
                    }

                    await _context.SaveChangesAsync();
                    return Ok(new { 
                         message ="Owner account reactivated and set to pending status.",
                       id= deletedOwner.Id,
                    }
                    );
                }

            }


            var rejectedOwner = await _context.Owners.Include(o => o.Request)
                .FirstOrDefaultAsync(o => o.Email == owner.Email && o.Request.status == Status.Rejected);

            if (rejectedOwner != null)
            {
                rejectedOwner.UserName = owner.UserName;
                rejectedOwner.FName = owner.FName;
                rejectedOwner.LName = owner.LName;
                rejectedOwner.PasswordHash = passwordHasher.HashPassword(rejectedOwner, owner.Password);
                rejectedOwner.Address = owner.Address;
                rejectedOwner.PhoneNumber = owner.PhoneNumber;
                rejectedOwner.Shop_Name = owner.Shop_Name;
                rejectedOwner.Shop_Description = owner.Shop_Description;
                rejectedOwner.Image = datastream.ToArray();
                rejectedOwner.Roles = Roles.Owner;

                if (rejectedOwner.Request != null && rejectedOwner.Request.status == Status.Accepted)
                {
                    rejectedOwner.Request.status = Status.Pending;
                }
                rejectedOwner.EmailConfirmed = false;
                await _context.SaveChangesAsync();
                return Ok(new { 
                    message="Owner account updated and request status set to pending." ,
                    id=rejectedOwner.Id
                
                
                });
            }

            if (ModelState.IsValid)
            {
                Owner newOwner = new()
                {
                    UserName = owner.UserName,
                    Email = owner.Email,
                    PasswordHash = owner.Password,
                    FName = owner.FName,
                    LName = owner.LName,
                    Address = owner.Address,
                    PhoneNumber = owner.PhoneNumber,
                    Shop_Name = owner.Shop_Name,
                    Shop_Description = owner.Shop_Description,
                    Image = datastream.ToArray(),
                    Roles = Roles.Owner,
                };

                var result = await _userManager.CreateAsync(newOwner, owner.Password);

                if (result.Succeeded)
                {
                    Request request = new()
                    {
                        status = Status.Pending,
                        RejectionReason = string.Empty,
                        OwnerId = newOwner.Id
                    };

                    _context.Requests.Add(request);
                    await _context.SaveChangesAsync();
                    return Ok(new { 
                        message="New owner and request created successfully." ,
                        id=newOwner.Id
                    
                    
                    });
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                }
            }

            return BadRequest(ModelState);
        }
        [HttpGet("OwnerProfile/{ownerId}")]
        public async Task<IActionResult> GetOwnerDetails(string ownerId)
        {

            if (string.IsNullOrEmpty(ownerId))
            {
                return BadRequest("OwnerId is required.");
            }

           


            var owner = await _context.Owners
                .FirstOrDefaultAsync(o => o.Id == ownerId && !o.IsDelete);

            if (owner == null)
            {
                return NotFound("Owner not found.");
            }

            var request = await _context.Requests
                .FirstOrDefaultAsync(r => r.OwnerId == owner.Id);

            var ownerDetails = new OwnerProfileDTO
            {
                Id = owner.Id,
                FName = owner.FName,
                LName = owner.LName,
                Email = owner.Email,
                UserName = owner.UserName,
                Address = owner.Address,
                PhoneNumber = owner.PhoneNumber,
                RegistirationDate = owner.RegistirationDate,
                ImageBase64 = owner.Image != null ? Convert.ToBase64String(owner.Image) : null,
                Shop_Name = owner.Shop_Name,
                Shop_description = owner.Shop_Description,

            };

            return Ok(ownerDetails);
        }
        [HttpPut("Update_Owner/{OwnerId}")]
        public async Task<IActionResult> UpdateOwner([FromForm] UpdateOwnerDto updateOwnerDto,string OwnerId)
        {


            var existingOwner = await _context.Owners
                .FirstOrDefaultAsync(o => o.Id == OwnerId);

            if (existingOwner == null)
            {
                return NotFound("Owner not found.");
            }


            var oldData = new
            {
                existingOwner.UserName,
                existingOwner.Email,
                existingOwner.FName,
                existingOwner.LName,
                existingOwner.Address,
                existingOwner.PhoneNumber,
                existingOwner.Shop_Name,
                existingOwner.Shop_Description,
                existingOwner.Image
            };


            existingOwner.UserName = updateOwnerDto.UserName ?? existingOwner.UserName;
            existingOwner.Email = updateOwnerDto.Email ?? existingOwner.Email;
            existingOwner.FName = updateOwnerDto.FName ?? existingOwner.FName;
            existingOwner.LName = updateOwnerDto.LName ?? existingOwner.LName;
            existingOwner.Address = updateOwnerDto.Address ?? existingOwner.Address;
            existingOwner.PhoneNumber = updateOwnerDto.PhoneNumber ?? existingOwner.PhoneNumber;
            existingOwner.Shop_Name = updateOwnerDto.Shop_Name ?? existingOwner.Shop_Name;
            existingOwner.Shop_Description = updateOwnerDto.Shop_description ?? existingOwner.Shop_Description;

            if (updateOwnerDto.Image != null)
            {
                if (!_allowedextension.Contains(Path.GetExtension(updateOwnerDto.Image.FileName).ToLower()) ||
                    updateOwnerDto.Image.Length > postersize)
                {
                    return BadRequest("Only tif, jpg, jpeg images are allowed, or the file size is too large.");
                }

                using var datastream = new MemoryStream();
                await updateOwnerDto.Image.CopyToAsync(datastream);
                existingOwner.Image = datastream.ToArray();
            }


            await _context.SaveChangesAsync();


            return Ok("Owner data updated successfully.");


        }
        [HttpPatch("Update_photo/{ownerId}")]
        public async Task<IActionResult> UpdatePhoto([FromForm] UpdatephotoDTO updatephoto, string ownerId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(o => o.Id == ownerId && !o.IsDelete);

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
      




       
        [HttpGet("GetCategories")]
        public async Task<IActionResult> GetCategories()
        {
            
            var categoriesList = Enum.GetNames(typeof(Categories)).ToList();
            return Ok(categoriesList);

           
        }


        [HttpPost("AddColor")]
        public async Task<IActionResult> AddColor([FromForm] ColorDTO colorDto)
        {
           
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState); // Returns validation errors
            }
            bool colorExists = await _context.Colors
    .AnyAsync(c => c.Color_Name.ToLower() == colorDto.Color_Name.ToLower());

            if (colorExists)
            {
                return BadRequest("This color already exists.");
            }

            Models.Color color = new()
            {
                Color_Name = colorDto.Color_Name
            };

            _context.Colors.Add(color);
            await _context.SaveChangesAsync();

            return Ok("Color added successfully");

        }
        [HttpGet("GetColors")]
        public async Task<IActionResult> GetColors()
        {
           
            var colors = await _context.Colors
                .Select(c => new { c.Id, c.Color_Name })
                .ToListAsync();
            if (colors.Count == 0)
            {
                return NotFound();
            }

            return Ok(colors);
        }
        [HttpPost("AddSize")]
        public async Task<IActionResult> AddSize([FromForm] string Size_name)
        {
           
            if (string.IsNullOrEmpty(Size_name))
            {
                return BadRequest("Invaild Input");
            }
            Models.Size size = new()
            {
                Size_Name = Size_name
            };
            _context.Sizes.Add(size);

            // Save all changes in a single transaction
            await _context.SaveChangesAsync();

            return Ok("Size added successfully");

        }
        [HttpGet("GetSizes")]
        public async Task<IActionResult> GetSizes()
        {
            //var userRole = HttpContext.Session.GetString("UserRole");
            //Console.WriteLine(userRole);

            //if (string.IsNullOrEmpty(userRole) || userRole != "Owner")
            //{
            //    return Forbid("Only owners can show sizes."); 
            //}
            var sizes = await _context.Sizes
                .Select(s => new { s.Id, s.Size_Name })
                .ToListAsync();
            if (sizes.Count == 0)
            {
                return NotFound();
            }

            return Ok(sizes);
        }


        [HttpPost("AddStock")]
        public async Task<IActionResult> AddStock([FromForm] StockDTO stockDTO)
        {
            //var userRole = HttpContext.Session.GetString("UserRole");
            //Console.WriteLine(userRole);

            //if (string.IsNullOrEmpty(userRole) || userRole != "Owner")
            //{
            //    return Forbid("Only owners can add stocks."); // 403 Forbidden
            //}
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var product = await _context.Products
                  .Include(p => p.owner) // Include owner details
                  .FirstOrDefaultAsync(p => p.Product_Id == stockDTO.Product_Id);
            var isProductValid = await _context.Products.AnyAsync(p => p.Product_Id == stockDTO.Product_Id);
            var isColorValid = await _context.Colors.AnyAsync(c => c.Id == stockDTO.Color_id);

            if (!isProductValid)
            {
                return BadRequest("Invalid Product ID.");
            }

            if (!isColorValid)
            {
                return BadRequest("Invalid Color ID.");
            }


            var isStockExists = await _context.Stocks.AnyAsync(s =>
                s.Color_id == stockDTO.Color_id &&
                s.Size_ID == stockDTO.Size_Id &&
                s.Product_Id == stockDTO.Product_Id);

            if (isStockExists)
            {
                return Conflict("Stock with the same product, size, and color already exists.");
            }

            // Create and save the stock


            Stock stock = new()
            {
                Color_id = stockDTO.Color_id,
                Size_ID = stockDTO.Size_Id,
                Product_Id = stockDTO.Product_Id,
                Quantity = stockDTO.Quantity,
            };
            _context.Stocks.Add(stock);
            await _context.SaveChangesAsync();
            // Check if owner is valid and send notifications
            if (product.owner != null)
            {
                var usersToNotify = await _context.FavListShops
                    .Where(fls => fls.Owner_Id == product.owner.Id)
                    .Select(fls => fls.FavList.User_Id)
                    .ToListAsync();

                // Fetch color and size names
                var color = await _context.Colors.FirstOrDefaultAsync(c => c.Id == stockDTO.Color_id);
                var size = await _context.Sizes.FirstOrDefaultAsync(s => s.Id == stockDTO.Size_Id);

                string colorName = color?.Color_Name ?? "Unknown Color";
                string sizeName = size?.Size_Name ?? "Unknown Size";

                // Prepare and send notifications to users
                foreach (var userId in usersToNotify)
                {
                    try
                    {
                        var notification = new Notification
                        {
                            Title = "New Stock Added!",
                            Body = $"NOW Product: {product.Product_Name} from Shop {product.owner.Shop_Name} is available with Color: {colorName} and Size :{sizeName}",
                            DateTime = DateTime.Now,
                            isread = false,
                            User_Id = userId,
                            Notification_Message = $"Owner '{product.owner.Shop_Name}' added new stock for the product '{product.Product_Name}'."
                            ,
                            Image = product.owner.Image // Add the shop photo

                        };

                        _context.Notifications.Add(notification);
                        await _context.SaveChangesAsync();  // Save notification to the database

                        // Send notification via SignalR (async)



                        Console.WriteLine($"Notification sent to user {userId}.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error sending notification to user {userId}: {ex.Message}");
                    }
                }
                foreach (var userId in usersToNotify)
                {
                   
                        var notification = await _context.Notifications
                            .OrderByDescending(n => n.Notification_Id)
                            .FirstOrDefaultAsync(n => n.User_Id == userId && n.Body.Contains(product.Product_Name)); // Match specific notification

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
            return Ok(new { Message = "Stock added successfully", StockId = stock.Stock_Id });
        }
        [HttpGet("GetAllStock/{productid}")]
        public async Task<IActionResult> GetAllStock(int productid)
        {
            //var userRole = HttpContext.Session.GetString("UserRole");
            //Console.WriteLine(userRole);

            //if (string.IsNullOrEmpty(userRole) || userRole != "Owner")
            //{
            //    return Forbid("Only owners can add stocks."); // 403 Forbidden
            //}
            var stocks = await _context.Stocks
     .Where(s => s.Product_Id == productid) // Filter by productid
     .Select(s => new
     {
         s.Color_id,
         s.color.Color_Name,
         s.Size_ID,
         s.size.Size_Name,
         s.Quantity
     }).ToListAsync();
            if (stocks.Count == 0)
            {
                return NotFound();
            }

            return Ok(stocks);
        }
        [HttpGet("Owner/{ownerId}/Count")]
        public async Task<IActionResult> GetFavoritesCountByOwner(string ownerId)
        {

            var count = await _context.FavListShops
                .Where(f => f.Owner_Id == ownerId)
                .Select(f => f.FavList.User_Id)
                .Distinct()
                .CountAsync();

            return Ok(new { OwnerId = ownerId, Countofusers = count });


        }
        [HttpPut("UpdateExistingImage/{imageId}")]
        public async Task<IActionResult> UpdateExistingImage(int imageId, [FromForm] ImageUpdateDTO updatedPhoto)
        {
            if (imageId <= 0)
            {
                return BadRequest("Image ID is required.");
            }

            // Fetch the existing image by ID
            var existingImage = await _context.Images.FirstOrDefaultAsync(img => img.Image_Id == imageId);

            if (existingImage == null)
            {
                return NotFound($"No image found with ID: {imageId}");
            }

            // Ensure the new image file is provided
            if (updatedPhoto == null || updatedPhoto.Photo == null || updatedPhoto.Photo.Length == 0)
            {
                return BadRequest("Updated photo file is required and must not be empty.");
            }

            // Update the image data
            using (var memoryStream = new MemoryStream())
            {
                if (!_allowedextension.Contains(Path.GetExtension(updatedPhoto.Photo.FileName).ToLower()) || updatedPhoto.Photo.Length > postersize)
                    return BadRequest("Only .tif, .jpg, .jpeg images are allowed, or file size exceeds the limit.");
                await updatedPhoto.Photo.CopyToAsync(memoryStream); // Copy the file to memory stream
                existingImage.Photo = memoryStream.ToArray();       // Update the byte array
            }

            // Save changes to the database
            _context.Images.Update(existingImage);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Image updated successfully.",
                UpdatedImage = new
                {
                    existingImage.Image_Id,
                    Base64Photo = Convert.ToBase64String(existingImage.Photo)
                }
            });
        }



       
      
        [HttpPost("AddMulti-Images/{productid}")]
        public async Task<IActionResult> NewPhotos(int productid, [FromForm] NewImageDTO image)
        {
            // Retrieve the product
            var product = await _context.Products.FindAsync(productid);
            if (product == null)
            {
                return NotFound(new { Message = "Product not found." });
            }

            // Check if new photos are provided
            if (image.NewPhotos == null || !image.NewPhotos.Any())
            {
               
                return BadRequest(new { Message = "No photos provided." });
            }

            // Process and save each photo
            foreach (var photo in image.NewPhotos)
            {
                if (!_allowedextension.Contains(Path.GetExtension(photo.FileName).ToLower()) || photo.Length > postersize)
                    return BadRequest("Only .tif, .jpg, .jpeg images are allowed, or file size exceeds the limit.");
                using (var memoryStream = new MemoryStream())
                {
                    await photo.CopyToAsync(memoryStream);
                    var newImage = new Models.Image
                    {
                        Product_Id = product.Product_Id,
                        Photo = memoryStream.ToArray()
                    };
                    _context.Images.Add(newImage);
                }
            }

            await _context.SaveChangesAsync();

            // Return success response with Base64-encoded images
            return Ok(new
            {
                Message = "images added successfully.",
                UpdatedImage = new
                {
                    Images = image.NewPhotos.Select(photo => new
                    {
                        FileName = photo.FileName,
                        Base64Photo = Convert.ToBase64String((new MemoryStream()).ToArray())
                    }).ToList()
                }
            });
        }
        [HttpPatch("ReplaceImage/{ProductId}")]
        public async Task<IActionResult> ReplaceImage([FromForm] ReplaceImageDTO replaceImageDTO, int ProductId)
        {
            
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Product_Id == ProductId);
            if (product == null)
            {
                return NotFound($"No product found with ID: {ProductId}");
            }

            
            var existingImages = await _context.Images
                .Where(img => img.Product_Id == ProductId)
                .ToListAsync();

            if (existingImages == null || !existingImages.Any())
            {
                return NotFound("No images found for the given product.");
            }

           
            var imgToReplace = await _context.Images
                .FirstOrDefaultAsync(i => i.Image_Id == replaceImageDTO.ImageId);

            if (imgToReplace == null)
            {
                return NotFound($"No image found with ID: {replaceImageDTO.ImageId}");
            }

         
            var oldDefaultImage = product.DefualtImage; 
            product.DefualtImage = imgToReplace.Photo;  
            imgToReplace.Photo = oldDefaultImage;       

           
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Image replaced successfully.",
                ProductId,
                ReplacedImageId = replaceImageDTO.ImageId,
                oldDefaultImage,
                product.DefualtImage

            });
        }


    }
}
