namespace Vstore.Models
{
    public class Owner: User
    {
        public string Shop_Name { get; set; }
        public string Shop_Description { get; set; }
       
        public ICollection<Product> Products { get; set; }
       // public ICollection<Categories> Categories { get; set; }

        public ICollection<FavListShop> FavListShops { get; set; }
        public Request Request { get; set; }

    }
}
