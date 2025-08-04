namespace Vstore.DTO
{
    public class AdminDTO
    {
       public string OwnerId { get; set; } 
        public string FName { get; set; }
        public string LName { get; set; }
        public string Email { get; set; }
        public string UserName { get; set; }
        public string Address { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime RegistirationDate { get; set; }
        public string? ImageBase64 { get; set; }
        public string Shop_Name { get; set; }
        public string Shop_Description { get; set; }

        public Status status { get; set; }
        public string? RejectionReason { get; set; }
        public string? Deletereason { get; set; }
        

    }


}
