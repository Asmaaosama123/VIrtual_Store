namespace Vstore.DTO
{
    public class ProductDetailsDTO
    {
        
           // public int Product_Id { get; set; }
            public string ProductName { get; set; }
            public float Product_Price { get; set; }
            public int Product_View { get; set; }
            public string Material { get; set; }
            public bool Has_Sale { get; set; }
            public int Sale_Percentage { get; set; }
        public string Description { get; set; }
        public string ShopId { get; set; }
        public string Type { get; set; }
            public Categories category{ get; set; }
        public string Category_Name { get; set; }
        public float Product_Price_after_sale { get; set; }
        public string defualtimage { get; set; }

        public List<PhotoDTO> Photos { get; set; }



    }
}
