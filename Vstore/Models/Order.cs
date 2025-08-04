using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Vstore.Models
{
    public enum Way { Cash, OnlinePayment}
    public class Order
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Order_Id { get; set; }

        public string User_Id { get; set; }
        public DateTime Date { get; set; }
     public Way PaymentMethod { get; set; }
        public User User { get; set; } 
       

        
        public ICollection<Order_Product> Order_Products { get; set; }
        public Payment Payment { get; set; }

    }
}
