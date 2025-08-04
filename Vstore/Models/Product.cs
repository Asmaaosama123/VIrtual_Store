using static System.Net.Mime.MediaTypeNames;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Vstore.Models
{
   public enum Categories { Men , Women, Kids}
    public class Product
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Product_Id { get; set; }
        public string Product_Name { get; set; }
        public float Product_Price { get; set; }
        public int Product_View { get; set; }
        public string Material { get; set; }
        public bool Has_Sale { get; set; }
        public int Sale_Percentage { get; set; }
        public string Product_Description { get; set; }
        public byte[] DefualtImage { get; set; }
        public bool IsDelete { get; set; } = false;
        public string ProductType { get; set; }
        public int Category_Id { get; set; }
        public string Owner_Id { get; set; }
        public ICollection<Order_Product> Order_Products { get; set; }
        public ICollection<Stock> Stock { get; set; }
        public ICollection<Image> Images { get; set; }
        public Categories Category { get; set; }
     //   public Category category { get; set; }
        public Owner owner { get; set; }
        public ICollection<Rate> Rate { get; set; }
    }
}
