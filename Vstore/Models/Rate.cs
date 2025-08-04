namespace Vstore.Models
{
    public class Rate
    {
        public int Product_Id { get; set; }
        public string User_Id { get; set; }
        public decimal Rating { get; set; }
        public Product Product { get; set; }
        public User User { get; set; }
    }
}
