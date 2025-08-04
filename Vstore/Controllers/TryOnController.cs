using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Vstore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TryOnController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly AppDBContext _dbContext;

        public TryOnController(HttpClient httpClient, AppDBContext dbContext)
        {
            _httpClient = httpClient;
            _dbContext = dbContext;
        }

        [HttpPost("tryon/{imageid}")]
        public async Task<IActionResult> TryOn([FromForm] TryOnRequest request, int imageid)
        {
            try
            {
                if (request.UserImage == null)
                    return BadRequest(new { error = "User image is required." });

              
                string userImageBase64 = await ConvertToBase64(request.UserImage);

               
                string clothingImageBase64 = await GetClothingImageBase64(imageid);
                if (string.IsNullOrEmpty(clothingImageBase64))
                    return NotFound(new { error = "Clothing image not found." });

                var data = new
                {
                    crop = false,
                    seed = 42,
                    steps = 30,
                    category = request.tryoncategory.ToString(),
                    force_dc = false,
                    human_img = userImageBase64,
                    garm_img = clothingImageBase64,
                    mask_only = false,
                    garment_des = "Fetched Clothing"
                };

                var json = JsonSerializer.Serialize(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Add("x-api-key", "SG_2b97f88a6044ad45");

                var response = await _httpClient.PostAsync("https://api.segmind.com/v1/idm-vton", content);

               
                var contentType = response.Content.Headers.ContentType.MediaType;

                if (!response.IsSuccessStatusCode)
                {
                    string errorResponse = await response.Content.ReadAsStringAsync();
                    return BadRequest(new { error = errorResponse });
                }


                if (contentType.StartsWith("image"))
                {
                    var imageBytes = await response.Content.ReadAsByteArrayAsync();
                    return Ok( imageBytes);
                }

                return BadRequest(new { error = "Unexpected response format." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        private async Task<string> ConvertToBase64(IFormFile file)
        {
            using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms);
                return Convert.ToBase64String(ms.ToArray());
            }
        }

        private async Task<string> GetClothingImageBase64(int imageid)
        {
           
            var imageEntity = await _dbContext.Images.FindAsync(imageid);
            if (imageEntity == null || imageEntity.Photo == null)
                return null;

           
            return Convert.ToBase64String(imageEntity.Photo);
        }
    }
    public enum tryoncategory { upper_body, lower_body, dresses }
    public class TryOnRequest
    {
     
        public IFormFile UserImage { get; set; }
        public tryoncategory tryoncategory { get; set; }
    }

}
