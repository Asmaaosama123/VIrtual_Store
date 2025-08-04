using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
    public class Hunyuan3DController : ControllerBase
    {
        private readonly HttpClient _httpClient;

        public Hunyuan3DController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        [HttpPost("generate3D")]
        public async Task<IActionResult> Generate3DModel([FromForm] ImageUploadRequest request)
        {
            try
            {
                if (request.ImageFile == null && string.IsNullOrEmpty(request.ImageUrl))
                    return BadRequest(new { error = "Please provide either an image file or an image URL." });

                // Convert image to Base64
                string imageBase64 = request.ImageFile != null
                    ? await ConvertToBase64(request.ImageFile)
                    : await ConvertUrlToBase64(request.ImageUrl);

                var payload = new
                {
                    image = imageBase64,
                    octree_resolution = 256,
                    num_inference_steps = 30,
                    guidance_scale = 5.5,
                    seed = 12467,
                    face_count = 40000,
                    texture = true
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("x-api-key", "SG_2b97f88a6044ad45");

                var response = await _httpClient.PostAsync("https://api.segmind.com/v1/hunyuan-3d-2", content);
                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return BadRequest(new { error = responseString });

                // Parse JSON response
                var result = JsonSerializer.Deserialize<ApiResponse>(responseString);

                if (result == null || string.IsNullOrEmpty(result.output))
                    return BadRequest(new { error = "Invalid response from API" });

                return Ok(new
                {
                    success = true,
                    message = "3D Model generated successfully.",
                    apiResponse = responseString,  // Full JSON response as a string
                    fileUrl = result.output  // URL to download the GLB file
                });
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

        private async Task<string> ConvertUrlToBase64(string imageUrl)
        {
            var imageBytes = await _httpClient.GetByteArrayAsync(imageUrl);
            return Convert.ToBase64String(imageBytes);
        }
    }

    public class ImageUploadRequest
    {
        public IFormFile? ImageFile { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class ApiResponse
    {
        public string output { get; set; }
    }
}
