namespace Vstore.DTO
{
    public class OwnerProfileDTO
    {
        public string Id { get; set; }
        public string FName { get; set; }
        public string LName { get; set; }
        public string Email { get; set; }
        public string UserName { get; set; }
        public string Address { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime RegistirationDate { get; set; }
        public string? ImageBase64 { get; set; }
        public string Shop_Name { get; set; }
        public string Shop_description { get; set; }
    }
}
