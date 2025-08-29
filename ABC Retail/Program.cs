using ABC_Retail.Services;
using Microsoft.Extensions.Logging.Console;

var builder = WebApplication.CreateBuilder(args);

// Configure logging to use console only and avoid Event Log issues
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "HH:mm:ss ";
});

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure Azure Storage services
var connectionString = builder.Configuration.GetConnectionString("AzureStorage") ?? 
                      builder.Configuration["AzureStorage:ConnectionString"];

// Register Azure Storage services
builder.Services.AddSingleton(provider =>
{
    return new Azure.Storage.Blobs.BlobServiceClient(connectionString);
});

builder.Services.AddSingleton(provider =>
{
    return new Azure.Storage.Queues.QueueServiceClient(connectionString);
});

builder.Services.AddSingleton(provider =>
{
    return new Azure.Data.Tables.TableServiceClient(connectionString);
});

builder.Services.AddSingleton(provider =>
{
    return new Azure.Storage.Files.Shares.ShareServiceClient(connectionString);
});

// Register custom services
builder.Services.AddScoped<IAzureTableService, AzureTableService>();
builder.Services.AddScoped<IAzureBlobService, AzureBlobService>();
builder.Services.AddScoped<IAzureQueueService, AzureQueueService>();
builder.Services.AddScoped<IAzureFileService, AzureFileService>();
builder.Services.AddScoped<IInMemoryProductService, InMemoryProductService>();

// Configure South African culture
var cultureInfo = new System.Globalization.CultureInfo("en-ZA");
System.Globalization.CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
