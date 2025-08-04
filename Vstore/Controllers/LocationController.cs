using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vstore.Models;
using Vstore.DTO;

namespace Vstore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LocationController : ControllerBase
    {
        private readonly AppDBContext _context;

        public LocationController(AppDBContext context)
        {
            _context = context;
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUserLocation(string userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound("User not found");

            return Ok(new
            {
                user.Id,
                user.FName,
                user.LName,
                user.Latitude,
                user.Longitude
            });
        }

        [HttpPost]
        public async Task<IActionResult> SaveUserLocation([FromBody] UserLocationDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _context.Users.FindAsync(dto.UserId);
            if (user == null)
                return NotFound("User not found");

            user.Latitude = dto.Latitude;
            user.Longitude = dto.Longitude;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Location updated successfully",
                user.Latitude,
                user.Longitude
            });
        }
        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371; // Radius of the Earth in km
            var latRad1 = DegreesToRadians(lat1);
            var lonRad1 = DegreesToRadians(lon1);
            var latRad2 = DegreesToRadians(lat2);
            var lonRad2 = DegreesToRadians(lon2);

            var dLat = latRad2 - latRad1;
            var dLon = lonRad2 - lonRad1;

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(latRad1) * Math.Cos(latRad2) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private double DegreesToRadians(double deg) => deg * (Math.PI / 180);

        [HttpGet("distance")]
        public async Task<IActionResult> GetDistance([FromQuery] string fromUserId, [FromQuery] string toUserId)
        {
            var fromUser = await _context.Users.FindAsync(fromUserId);
            var toUser = await _context.Users.FindAsync(toUserId);

            if (fromUser == null || toUser == null)
                return NotFound("One or both users not found.");

            var distance = CalculateDistance(
      Convert.ToDouble(fromUser.Latitude),
      Convert.ToDouble(fromUser.Longitude),
      Convert.ToDouble(toUser.Latitude),
      Convert.ToDouble(toUser.Longitude)
  );


            // 50 pounds per 100 km
            var cost = Math.Ceiling(distance / 100) * 50;

            return Ok(new
            {
                DistanceInKm = distance,
                EstimatedCostInPounds = $"{cost} pounds"
            });
        }

    }
}