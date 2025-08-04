using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Vstore.Models
{
    public class Color
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required]
        [RegularExpression("^[a-zA-Z ]*$", ErrorMessage = "Only English letters are allowed.")]
        public string Color_Name { get; set; }
        public ICollection<Stock> Stock { get; set; }
    }
}
