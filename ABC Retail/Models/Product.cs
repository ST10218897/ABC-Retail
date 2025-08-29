using Azure;
using Azure.Data.Tables;

namespace ABC_Retail.Models
{
    public class Product : ITableEntity
    {
        public string ProductId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Price { get; set; } = "0.00";
        public int StockQuantity { get; set; }
        public string Category { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        // ITableEntity implementation
        public string PartitionKey { get; set; } = "Products";
        public string RowKey { get => ProductId; set => ProductId = value; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
