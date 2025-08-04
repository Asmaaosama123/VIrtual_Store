using System.ComponentModel.DataAnnotations;

namespace Vstore.DTO
{
    public class NewUser
    {

        [Required(ErrorMessage = "First Name is required.")]
        [StringLength(30, ErrorMessage = "First Name cannot exceed 30 characters.")]
        [RegularExpression(@"^[a-zA-Z]+$", ErrorMessage = "First Name can only contain alphabetic characters.")]
        public string FName { get; set; }

        [Required(ErrorMessage = "Last Name is required.")]
        [StringLength(30, ErrorMessage = "Last Name cannot exceed 30 characters.")]
        [RegularExpression(@"^[a-zA-Z]+$", ErrorMessage = "Last Name can only contain alphabetic characters.")]
        public string LName { get; set; }
        [Required(ErrorMessage = "Username is required.")]
      //  [RegularExpression(@"^[a-zA-Z0-9_!@#$%^&()\-+=<>?]+$", ErrorMessage = "Username can only contain alphanumeric characters, underscores, and special characters like !@#$%^&()-+=<>?.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 100 characters.")]
        public string UserName { get; set; }
        [Required]
        [EmailAddress]
      //  [RegularExpression(@"^[a-zA-Z0-9._%+-]+@gmail\.com$", ErrorMessage = "Invalid Email Address. Only '@gmail.com' addresses are allowed.")]
        public string Email { get; set; }
        [Required]
        [DataType(DataType.Password)]
       // [RegularExpression(@"^(?=.[a-z])(?=.[A-Z])(?=.\d)(?=.[@$!%?&])[A-Za-z\d@$!%?&]{8,}$",
   // ErrorMessage = "Password must be at least 8 characters long and include at least one uppercase letter, one lowercase letter, one number, and one special character.")]
        public string Password { get; set; }
        [Required]
        [DataType(DataType.Password)]
        [Compare("Password")] // Use your custom attribute here
        public string ConfirmPassword { get; set; }
        [Required(ErrorMessage = "Address is required.")]
        [StringLength(200, ErrorMessage = "Address cannot exceed 200 characters.")]
        [RegularExpression(@"^(?![\d\W]+$)(?![\W\d]+$)(?=.*[a-zA-Z]).+", ErrorMessage = "Address must contain at least one letter and cannot consist of only numbers and special characters.")]
        public string Address { get; set; }
        [Required]
        [RegularExpression(@"^\d{1,11}$", ErrorMessage = "Phone number must be numeric and cannot exceed 11 digits.")]
        public string PhoneNumber { get; set; }

        public IFormFile Image
        {get; set;}
        }
}
