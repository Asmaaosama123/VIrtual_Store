namespace Vstore.Models
{
    public class Payment
    {
       public int Id { get; set; }
        public string StripeSessionId { get; set; }
        public string Currency {  get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
      public int OrderId { get; set; }
        public DateTime CreatedAt { get; set; }
        public Order Order { get; set; }
    }
}
