namespace Vstore.DTO
{
    public class CatItemsDTO
    {
        public int CartItemId { get; set; }
        public int productid { get; set; }
        public string productname { get; set; }
        public int TotalQuentity { get; set; }
        public float totalprice { get; set; }
        public string size { get; set; }
        public string color { get; set; }
        public float price { get; set; }
        public float priceAfterSelling { get; set; }
        public bool hasSale { get; set; }
        public int Stock_Id { get; set; }
        public string? ImageBase64 { get; set; }
        public int Quantity { get; set; }
        public string ShopId { get; set; }

    }
}