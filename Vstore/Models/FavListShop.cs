using System.Diagnostics;

namespace Vstore.Models
{
    public class FavListShop
    {
        public int FavList_Id { set; get; }
        public string Owner_Id { set; get; }

        public FavList FavList { get; set; }
        public Owner Owner { get; set; }
    }
}
