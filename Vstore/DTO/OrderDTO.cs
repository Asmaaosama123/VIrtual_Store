public class OrderDTO
{
    public int Order_Id { get; set; }
    public string User_Id { get; set; }
    public float TotalPrice { get; set; }  // Add this line
    public int TotalQuantity { get; set; }
    public Way PaymentMethod { get; set; }


    public List<ProductOrderDTO> Order_Products { get; set; }
}
