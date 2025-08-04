namespace Vstore.DTO
{

    public class UpdateOwnerDto
    {
      //  public string Id { get; set; }
        public string? FName { get; set; }
        public string? LName { get; set; }
        public string? Email { get; set; }
        public string? UserName { get; set; }

        [DataType(DataType.Password)]
        public string? Password { get; set; }

        [DataType(DataType.Password)]
        [Compare("Password")]
        public string? ConfirmPassword { get; set; }
        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Shop_Name { get; set; }
        public string? Shop_description { get; set; }

        public IFormFile? Image { get; set; }
    }

}