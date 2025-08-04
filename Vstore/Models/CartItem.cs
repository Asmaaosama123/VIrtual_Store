using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vstore.Models
{
    public class CartItem
    {
        public int CartId { get; set; }
       public int ProductId { get; set; }
        public int Quantity { get; set; }
        public int? StockId { get; set; }
        public bool IsDelete { get; set; }
        public Cart Cart { get; set; }
        public Product Product { get; set; }
        public Stock Stock { get; set; }
    }
}
