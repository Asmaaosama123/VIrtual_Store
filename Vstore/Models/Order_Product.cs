namespace Vstore.Models
{
    public class Order_Product
    {
        public int Order_Id { get; set; }
        public int Product_Id { get; set; }
        public int Quantity { get; set; }
        public int StockId { get; set; }
        public bool IsDelete { get; set; }
        public Order Order { get; set; }
        public Product Product { get; set; }
        public Stock Stock { get; set; }
    }
}
