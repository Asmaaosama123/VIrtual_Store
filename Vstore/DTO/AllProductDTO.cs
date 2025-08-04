namespace Vstore.DTO
{
    public class AllProductDTO
    {
       public int product_Id { get; set; }
        public string ProductName { get; set; }
        public float Product_Price { get; set; }
        public int Product_View { get; set; }
        public string Material { get; set; }
        public int Sale_Percentage { get; set; }
        public string Photo { get; set; }
        public float Product_Price_after_sale { get; set; }


        // public string OwnerId { get; set; }
    }
}
