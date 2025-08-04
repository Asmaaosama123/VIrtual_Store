using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Vstore.Models
{
    public class FavList
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int FavList_Id { get; set; }
        public string User_Id { get; set; }
        
        public User User { get; set; }
        public ICollection<FavListShop> FavListShops { get; set; }
    }
}
