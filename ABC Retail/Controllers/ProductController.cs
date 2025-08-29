using ABC_Retail.Models;
using ABC_Retail.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABC_Retail.Controllers
{
    public class ProductController : Controller
    {
        private readonly IAzureTableService _tableService;
        private readonly IAzureBlobService _blobService;
        private readonly IInMemoryProductService _productService;

        public ProductController(IAzureTableService tableService, IAzureBlobService blobService, IInMemoryProductService productService)
        {
            _tableService = tableService;
            _blobService = blobService;
            _productService = productService;
        }

        // GET: Product
        public async Task<IActionResult> Index(string category = "")
        {
            List<Product> products;
            
            try
            {
                // Try to get products from Azure Table Storage first
                if (string.IsNullOrEmpty(category))
                {
                    products = await _tableService.GetAllProductsAsync();
                }
                else
                {
                    products = await _tableService.GetProductsByCategoryAsync(category);
                }

                // If no products found in Azure, use in-memory service as fallback
                if (products == null || products.Count == 0)
                {
                    if (string.IsNullOrEmpty(category))
                    {
                        products = await _productService.GetAllProductsAsync();
                    }
                    else
                    {
                        products = await _productService.GetProductsByCategoryAsync(category);
                    }
                }
            }
            catch (Exception ex)
            {
                // If Azure storage fails, use in-memory service
                Console.WriteLine($"Azure Table Storage error: {ex.Message}. Using in-memory service.");
                
                if (string.IsNullOrEmpty(category))
                {
                    products = await _productService.GetAllProductsAsync();
                }
                else
                {
                    products = await _productService.GetProductsByCategoryAsync(category);
                }
            }

            ViewBag.Categories = await _productService.GetProductCategoriesAsync();
            ViewBag.SelectedCategory = category;

            return View(products);
        }

        // GET: Product/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var product = await _productService.GetProductAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // GET: Product/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Product/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description,Price,StockQuantity,Category,IsActive")] Product product, IFormFile imageFile)
        {
            if (ModelState.IsValid)
            {
                // Handle image upload
                if (imageFile != null && imageFile.Length > 0)
                {
                    var uploadedFile = await _blobService.UploadFileAsync(
                        imageFile, 
                        "product-images", 
                        product.Description, 
                        product.Category
                    );
                    product.ImageUrl = uploadedFile.BlobUrl;
                }

                Console.WriteLine($"Creating product: {product.Name}, Price: {product.Price}, Stock: {product.StockQuantity}");
                var success = await _tableService.AddProductAsync(product);
                if (success)
                {
                    return RedirectToAction(nameof(Index));
                }
                ModelState.AddModelError("", "Failed to create product.");
            }
            return View(product);
        }

        // GET: Product/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var product = await _tableService.GetProductAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: Product/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("ProductId,Name,Description,Price,StockQuantity,Category,ImageUrl,IsActive,CreatedDate,PartitionKey,RowKey,Timestamp,ETag")] Product product, IFormFile imageFile)
        {
            if (id != product.ProductId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // Handle image upload if a new file is provided
                if (imageFile != null && imageFile.Length > 0)
                {
                    var uploadedFile = await _blobService.UploadFileAsync(
                        imageFile, 
                        "product-images", 
                        product.Description, 
                        product.Category
                    );
                    product.ImageUrl = uploadedFile.BlobUrl;
                }

                var success = await _tableService.UpdateProductAsync(product);
                if (success)
                {
                    return RedirectToAction(nameof(Index));
                }
                ModelState.AddModelError("", "Failed to update product.");
            }
            return View(product);
        }

        // GET: Product/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var product = await _tableService.GetProductAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: Product/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var success = await _tableService.DeleteProductAsync(id);
            if (success)
            {
                return RedirectToAction(nameof(Index));
            }
            
            ModelState.AddModelError("", "Failed to delete product.");
            var product = await _tableService.GetProductAsync(id);
            return View("Delete", product);
        }

        private async Task<List<string>> GetProductCategories()
        {
            var products = await _tableService.GetAllProductsAsync();
            return products
                .Select(p => p.Category)
                .Where(c => !string.IsNullOrEmpty(c))
                .Distinct()
                .OrderBy(c => c)
                .ToList();
        }
    }
}
