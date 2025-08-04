using System.ComponentModel.DataAnnotations;

namespace Vstore.DTO
{
    public class Login
    {
        [Required]
        public string Email { get; set; }
        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
