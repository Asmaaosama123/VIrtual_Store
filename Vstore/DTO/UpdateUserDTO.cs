namespace Vstore.DTO
{
    public class UpdateUserDTO
    {
      //  public string Id { get; set; }
        public string? FName { get; set; }
        public string? LName { get; set; }
        public string? Email { get; set; }
        public string? UserName { get; set; }


        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }

        public IFormFile? Image { get; set; }
    }
}
