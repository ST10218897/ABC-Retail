using Azure;
using Azure.Data.Tables;
using ABC_Retail.Models;
using System.Collections.Concurrent;

namespace ABC_Retail.Services
{
    public interface IAzureTableService
    {
        Task<bool> AddCustomerAsync(Customer customer);
        Task<Customer?> GetCustomerAsync(string customerId);
        Task<List<Customer>> GetAllCustomersAsync();
        Task<bool> UpdateCustomerAsync(Customer customer);
        Task<bool> DeleteCustomerAsync(string customerId);

        Task<bool> AddProductAsync(Product product);
        Task<Product?> GetProductAsync(string productId);
        Task<List<Product>> GetAllProductsAsync();
        Task<List<Product>> GetProductsByCategoryAsync(string category);
        Task<bool> UpdateProductAsync(Product product);
        Task<bool> DeleteProductAsync(string productId);
    }

    public class AzureTableService : IAzureTableService
    {
        private readonly TableServiceClient _tableServiceClient;
        private readonly TableClient _customerTableClient;
        private readonly TableClient _productTableClient;

        private const string CustomersTableName = "Customers";
        private const string ProductsTableName = "Products";

        public AzureTableService(TableServiceClient tableServiceClient)
        {
            _tableServiceClient = tableServiceClient;
            _customerTableClient = _tableServiceClient.GetTableClient(CustomersTableName);
            _productTableClient = _tableServiceClient.GetTableClient(ProductsTableName);

            // Initialize tables if they don't exist
            InitializeTablesAsync().Wait();
        }

        private async Task InitializeTablesAsync()
        {
            try
            {
                await _tableServiceClient.CreateTableIfNotExistsAsync(CustomersTableName);
                await _tableServiceClient.CreateTableIfNotExistsAsync(ProductsTableName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing tables: {ex.Message}");
            }
        }

        #region Customer Operations

        public async Task<bool> AddCustomerAsync(Customer customer)
        {
            try
            {
                customer.CustomerId = Guid.NewGuid().ToString();
                customer.RowKey = customer.CustomerId;
                customer.PartitionKey = "Customers";

                await _customerTableClient.AddEntityAsync(customer);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding customer: {ex.Message}");
                return false;
            }
        }

        public async Task<Customer?> GetCustomerAsync(string customerId)
        {
            try
            {
                var response = await _customerTableClient.GetEntityAsync<Customer>("Customers", customerId);
                return response.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting customer: {ex.Message}");
                return null;
            }
        }

        public async Task<List<Customer>> GetAllCustomersAsync()
        {
            try
            {
                var customers = new List<Customer>();
                var query = _customerTableClient.QueryAsync<Customer>();

                await foreach (var customer in query)
                {
                    customers.Add(customer);
                }

                return customers;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting all customers: {ex.Message}");
                return new List<Customer>();
            }
        }

        public async Task<bool> UpdateCustomerAsync(Customer customer)
        {
            try
            {
                customer.PartitionKey = "Customers";
                customer.RowKey = customer.CustomerId;

                await _customerTableClient.UpdateEntityAsync(customer, ETag.All, TableUpdateMode.Replace);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating customer: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteCustomerAsync(string customerId)
        {
            try
            {
                await _customerTableClient.DeleteEntityAsync("Customers", customerId);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting customer: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Product Operations

        public async Task<bool> AddProductAsync(Product product)
        {
            try
            {
                product.ProductId = Guid.NewGuid().ToString();
                product.RowKey = product.ProductId;
                product.PartitionKey = "Products";

                await _productTableClient.AddEntityAsync(product);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding product: {ex.Message}");
                return false;
            }
        }

        public async Task<Product?> GetProductAsync(string productId)
        {
            try
            {
                var response = await _productTableClient.GetEntityAsync<Product>("Products", productId);
                return response.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting product: {ex.Message}");
                return null;
            }
        }

        public async Task<List<Product>> GetAllProductsAsync()
        {
            try
            {
                var products = new List<Product>();
                var query = _productTableClient.QueryAsync<Product>();

                await foreach (var product in query)
                {
                    products.Add(product);
                }

                return products;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting all products: {ex.Message}");
                return new List<Product>();
            }
        }

        public async Task<List<Product>> GetProductsByCategoryAsync(string category)
        {
            try
            {
                var products = new List<Product>();
                var query = _productTableClient.QueryAsync<Product>($"Category eq '{category}'");

                await foreach (var product in query)
                {
                    products.Add(product);
                }

                return products;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting products by category: {ex.Message}");
                return new List<Product>();
            }
        }

        public async Task<bool> UpdateProductAsync(Product product)
        {
            try
            {
                product.PartitionKey = "Products";
                product.RowKey = product.ProductId;

                await _productTableClient.UpdateEntityAsync(product, ETag.All, TableUpdateMode.Replace);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating product: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteProductAsync(string productId)
        {
            try
            {
                await _productTableClient.DeleteEntityAsync("Products", productId);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting product: {ex.Message}");
                return false;
            }
        }

        #endregion
    }
}
