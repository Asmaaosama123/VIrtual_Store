namespace Vstore.DTO
{
    public class ProductDTO
    {
       // Product Product= new Product();

        public string ProductName { get; set; }
        public float Product_Price { get; set; }
       
        public string Material { get; set; }
        public string Product_Description { get; set; }
        public string Product_Type { get; set; }
        public bool Has_Sale { get; set; }
        public int Sale_Percentage { get; set; }
        //public int Category_Id { get; set; }
        public Categories Categories { get; set; }

      public IFormFile Photo { get; set; }
      //  public List<IFormFile>? NewPhotos { get; set; }
        //[Required]
        //public List<IFormFile> Photos { get; set; }
        // public string Owner_Id { get; set; }
    }
}
