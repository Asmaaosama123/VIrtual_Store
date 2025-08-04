namespace Vstore.DTO
{
    public class UpdateProductDTO
    {
        public string ProductName { get; set; }
        public float Product_Price { get; set; }

        public string Material { get; set; }
        public string Product_Description { get; set; }
        public string Product_Type { get; set; }
        public bool Has_Sale { get; set; }
        public int Sale_Percentage { get; set; }
        //public int Category_Id { get; set; }
        public Categories Categories { get; set; }

      
    }
}
