namespace Vstore.DTO
{
    public class UserProfileDTO
    {
        public string id { get; set; }
        public string FName { get; set; }
        public string LName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string UserName { get; set; }

        public string Address { get; set; }
        public string PhoneNumber { get; set; }
        public string? ImageBase64 { get; set; }
    }
}
