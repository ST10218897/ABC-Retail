using ABC_Retail.Models;
using ABC_Retail.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABC_Retail.Controllers
{
    public class OrderController : Controller
    {
        private readonly IAzureQueueService _queueService;
        private readonly IAzureTableService _tableService;

        public OrderController(IAzureQueueService queueService, IAzureTableService tableService)
        {
            _queueService = queueService;
            _tableService = tableService;
        }

        // GET: Order
        public async Task<IActionResult> Index()
        {
            var queueLength = await _queueService.GetOrderQueueLengthAsync();
            ViewBag.QueueLength = queueLength;
            
            return View();
        }

        // GET: Order/Create
        public async Task<IActionResult> Create()
        {
            var customers = await _tableService.GetAllCustomersAsync();
            var products = await _tableService.GetAllProductsAsync();

            // Debug logging for products loaded
            Console.WriteLine($"Loaded {products.Count} products for order creation:");
            foreach (var product in products)
            {
                Console.WriteLine($"Product: {product.Name}, Price: {product.Price}, ID: {product.ProductId}");
            }

            // If no products are loaded from Azure Table Storage, add some sample products for testing
            if (products.Count == 0)
            {
                Console.WriteLine("No products found in Azure Table Storage. Adding sample products for testing.");
                products = new List<Product>
                {
                    new Product { ProductId = "sample-1", Name = "Sample Product 1", Price = "100.00", StockQuantity = 10, Category = "Electronics" },
                    new Product { ProductId = "sample-2", Name = "Sample Product 2", Price = "50.00", StockQuantity = 20, Category = "Clothing" },
                    new Product { ProductId = "sample-3", Name = "Sample Product 3", Price = "25.00", StockQuantity = 15, Category = "Books" }
                };
            }

            ViewBag.Customers = customers;
            ViewBag.Products = products;

            return View();
        }

        // POST: Order/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CustomerId,ProductId,Quantity,ShippingAddress,PaymentMethod,Notes")] Order order)
        {
            if (ModelState.IsValid)
            {
                // Get customer and product details
                var customer = await _tableService.GetCustomerAsync(order.CustomerId);
                var product = await _tableService.GetProductAsync(order.ProductId);

                if (customer == null || product == null)
                {
                    ModelState.AddModelError("", "Invalid customer or product selected.");
                    return await PopulateViewDataAndReturnView(order);
                }

                // Debug logging for price calculation
                Console.WriteLine($"Calculating TotalAmount: Product Price = {product.Price}, Quantity = {order.Quantity}");
                Console.WriteLine($"Order Quantity: {order.Quantity}, Product Price: {product.Price}");
                Console.WriteLine($"Product details - Name: {product.Name}, ID: {product.ProductId}, Price: {product.Price}");
                Console.WriteLine($"TotalAmount before calculation: {order.TotalAmount}");
                order.CustomerName = $"{customer.FirstName} {customer.LastName}";
                order.ProductName = product.Name;
                decimal productPrice = decimal.Parse(product.Price);
                order.TotalAmount = productPrice * order.Quantity;
                Console.WriteLine($"TotalAmount after calculation: {order.TotalAmount}");
                order.Status = "Pending";

                // Send order to queue
                var success = await _queueService.SendOrderMessageAsync(order);
                if (success)
                {
                    return RedirectToAction(nameof(Index));
                }
                ModelState.AddModelError("", "Failed to create order.");
            }

            return await PopulateViewDataAndReturnView(order);
        }

        // GET: Order/Process
        public async Task<IActionResult> Process()
        {
            var order = await _queueService.ReceiveOrderMessageAsync();
            
            if (order == null)
            {
                ViewBag.Message = "No orders in the queue.";
                return View();
            }

            // Store message details for later deletion
            ViewBag.MessageId = order.Notes; // Contains MessageId and PopReceipt

            return View(order);
        }

        // POST: Order/Complete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(string orderId, string status, string messageInfo)
        {
            if (!string.IsNullOrEmpty(messageInfo))
            {
                // Parse message info to get MessageId and PopReceipt
                var parts = messageInfo.Split(',');
                if (parts.Length == 2)
                {
                    var messageId = parts[0].Split(':')[1];
                    var popReceipt = parts[1].Split(':')[1];

                    // Use reflection or create a custom method to handle message deletion
                    // Since QueueMessage has read-only properties, we need to modify the service
                    // For now, we'll modify the AzureQueueService to accept messageId and popReceipt directly
                    await DeleteOrderMessageAsync(messageId, popReceipt);
                }
            }

            // Here you would typically update the order status in a database
            // For now, we'll just redirect to the index
            return RedirectToAction(nameof(Index));
        }

        private async Task DeleteOrderMessageAsync(string messageId, string popReceipt)
        {
            // This is a workaround since QueueMessage properties are read-only
            // In a real implementation, you might want to modify the service interface
            var queueClient = new Azure.Storage.Queues.QueueServiceClient(
                HttpContext.RequestServices.GetService<IConfiguration>()["AzureStorage:ConnectionString"]
            ).GetQueueClient("orders");

            await queueClient.DeleteMessageAsync(messageId, popReceipt);
        }

        // GET: Order/QueueStatus
        public async Task<IActionResult> QueueStatus()
        {
            var queueLength = await _queueService.GetOrderQueueLengthAsync();
            return Json(new { queueLength });
        }

        private async Task<IActionResult> PopulateViewDataAndReturnView(Order order)
        {
            var customers = await _tableService.GetAllCustomersAsync();
            var products = await _tableService.GetAllProductsAsync();

            // If no products are loaded from Azure Table Storage, add some sample products for testing
            if (products.Count == 0)
            {
                Console.WriteLine("No products found in Azure Table Storage. Adding sample products for testing.");
                products = new List<Product>
                {
                    new Product { ProductId = "sample-1", Name = "Sample Product 1", Price = "100.00", StockQuantity = 10, Category = "Electronics" },
                    new Product { ProductId = "sample-2", Name = "Sample Product 2", Price = "50.00", StockQuantity = 20, Category = "Clothing" },
                    new Product { ProductId = "sample-3", Name = "Sample Product 3", Price = "25.00", StockQuantity = 15, Category = "Books" }
                };
            }

            ViewBag.Customers = customers;
            ViewBag.Products = products;

            return View("Create", order);
        }
    }
}
