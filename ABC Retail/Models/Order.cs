using System.Text.Json.Serialization;

namespace ABC_Retail.Models
{
    public class Order
    {
        [JsonPropertyName("orderId")]
        public string OrderId { get; set; } = Guid.NewGuid().ToString();
        
        [JsonPropertyName("customerId")]
        public string CustomerId { get; set; } = string.Empty;
        
        [JsonPropertyName("customerName")]
        public string CustomerName { get; set; } = string.Empty;
        
        [JsonPropertyName("productId")]
        public string ProductId { get; set; } = string.Empty;
        
        [JsonPropertyName("productName")]
        public string ProductName { get; set; } = string.Empty;
        
        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }
        
        [JsonPropertyName("totalAmount")]
        public decimal TotalAmount { get; set; }
        
        [JsonPropertyName("orderDate")]
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        
        [JsonPropertyName("status")]
        public string Status { get; set; } = "Pending"; // Pending, Processing, Completed, Cancelled
        
        [JsonPropertyName("shippingAddress")]
        public string ShippingAddress { get; set; } = string.Empty;
        
        [JsonPropertyName("paymentMethod")]
        public string PaymentMethod { get; set; } = string.Empty;
        
        [JsonPropertyName("notes")]
        public string Notes { get; set; } = string.Empty;
    }
}
