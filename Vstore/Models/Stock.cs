using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Drawing;

namespace Vstore.Models
{
    public class Stock
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Stock_Id { get; set; }
        public int Color_id { get; set; }
        public int Size_ID { get; set; }
        public int Product_Id { get; set; }
        public int Quantity { get; set; }
        public Product Product { get; set; }
        [ForeignKey("Color_id")]
        public Color color { get; set; }
        public Size size { get; set; }
        public ICollection<CartItem> CartItems { get; set; }

    }
}
