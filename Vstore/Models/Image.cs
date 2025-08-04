using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Vstore.Models
{
    public class Image
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Image_Id { get; set; }
        public int Product_Id { get; set; }
        
        public byte[] Photo { get; set; }
        public Product Products { get; set; }
    }
}
