namespace Vstore.Models
{
    public class Cart
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CartId { get; set; }
        public string UserId { get; set; }
        public DateTime CreatedAt { get; set; }

       
        public User User { get; set; }
        public ICollection<CartItem> CartItems { get; set; }
    }
}
