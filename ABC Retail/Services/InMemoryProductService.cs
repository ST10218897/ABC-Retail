using ABC_Retail.Models;
using System.Collections.Concurrent;

namespace ABC_Retail.Services
{
    public interface IInMemoryProductService
    {
        Task<bool> AddProductAsync(Product product);
        Task<Product?> GetProductAsync(string productId);
        Task<List<Product>> GetAllProductsAsync();
        Task<List<Product>> GetProductsByCategoryAsync(string category);
        Task<bool> UpdateProductAsync(Product product);
        Task<bool> DeleteProductAsync(string productId);
        Task<List<string>> GetProductCategoriesAsync();
    }

    public class InMemoryProductService : IInMemoryProductService
    {
        private readonly ConcurrentDictionary<string, Product> _products = new ConcurrentDictionary<string, Product>();
        private int _productCounter = 1;

        public InMemoryProductService()
        {
            // Add some sample products for testing
            AddSampleProducts();
        }

        private void AddSampleProducts()
        {
            var sampleProducts = new List<Product>
            {
                new Product 
                { 
                    ProductId = "sample-1", 
                    Name = "Sample Product 1", 
                    Description = "A sample product for testing",
                    Price = "100.00", // Changed from decimal to string
                    StockQuantity = 10, 
                    Category = "Electronics",
                    CreatedDate = DateTime.Now
                },
                new Product 
                { 
                    ProductId = "sample-2", 
                    Name = "Sample Product 2", 
                    Description = "Another sample product for testing",
                    Price = "50.00", // Changed from decimal to string
                    StockQuantity = 20, 
                    Category = "Clothing",
                    CreatedDate = DateTime.Now
                },
                new Product 
                { 
                    ProductId = "sample-3", 
                    Name = "Sample Product 3", 
                    Description = "A third sample product for testing",
                    Price = "25.00", // Changed from decimal to string
                    StockQuantity = 15, 
                    Category = "Books",
                    CreatedDate = DateTime.Now
                }
            };

            foreach (var product in sampleProducts)
            {
                _products.TryAdd(product.ProductId, product);
            }
        }

        public Task<bool> AddProductAsync(Product product)
        {
            try
            {
                product.ProductId = $"prod-{_productCounter++}";
                product.CreatedDate = DateTime.Now;
                return Task.FromResult(_products.TryAdd(product.ProductId, product));
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        public Task<Product?> GetProductAsync(string productId)
        {
            _products.TryGetValue(productId, out var product);
            return Task.FromResult(product);
        }

        public Task<List<Product>> GetAllProductsAsync()
        {
            return Task.FromResult(_products.Values.OrderBy(p => p.Name).ToList());
        }

        public Task<List<Product>> GetProductsByCategoryAsync(string category)
        {
            var products = _products.Values
                .Where(p => p.Category?.Equals(category, StringComparison.OrdinalIgnoreCase) == true)
                .OrderBy(p => p.Name)
                .ToList();
            return Task.FromResult(products);
        }

        public Task<bool> UpdateProductAsync(Product product)
        {
            try
            {
                if (_products.ContainsKey(product.ProductId))
                {
                    _products[product.ProductId] = product;
                    return Task.FromResult(true);
                }
                return Task.FromResult(false);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        public Task<bool> DeleteProductAsync(string productId)
        {
            return Task.FromResult(_products.TryRemove(productId, out _));
        }

        public Task<List<string>> GetProductCategoriesAsync()
        {
            var categories = _products.Values
                .Select(p => p.Category)
                .Where(c => !string.IsNullOrEmpty(c))
                .Distinct()
                .OrderBy(c => c)
                .ToList();
            return Task.FromResult(categories);
        }
    }
}
