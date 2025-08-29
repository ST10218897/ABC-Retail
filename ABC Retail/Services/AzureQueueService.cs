using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using ABC_Retail.Models;
using System.Text.Json;

namespace ABC_Retail.Services
{
    public interface IAzureQueueService
    {
        Task<bool> SendOrderMessageAsync(Order order);
        Task<Order?> ReceiveOrderMessageAsync();
        Task<bool> DeleteOrderMessageAsync(QueueMessage message);
        Task<int> GetOrderQueueLengthAsync();
        Task<bool> SendInventoryMessageAsync(string messageContent);
        Task<string?> ReceiveInventoryMessageAsync();
        Task<bool> DeleteInventoryMessageAsync(QueueMessage message);
        Task<int> GetInventoryQueueLengthAsync();
    }

    public class AzureQueueService : IAzureQueueService
    {
        private readonly QueueServiceClient _queueServiceClient;
        private readonly QueueClient _orderQueueClient;
        private readonly QueueClient _inventoryQueueClient;

        private const string OrderQueueName = "orders";
        private const string InventoryQueueName = "inventory";

        public AzureQueueService(QueueServiceClient queueServiceClient)
        {
            _queueServiceClient = queueServiceClient;
            _orderQueueClient = _queueServiceClient.GetQueueClient(OrderQueueName);
            _inventoryQueueClient = _queueServiceClient.GetQueueClient(InventoryQueueName);

            // Initialize queues if they don't exist
            InitializeQueuesAsync().Wait();
        }

        private async Task InitializeQueuesAsync()
        {
            try
            {
                await _orderQueueClient.CreateIfNotExistsAsync();
                await _inventoryQueueClient.CreateIfNotExistsAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing queues: {ex.Message}");
            }
        }

        #region Order Queue Operations

        public async Task<bool> SendOrderMessageAsync(Order order)
        {
            try
            {
                var message = JsonSerializer.Serialize(order);
                Console.WriteLine($"Sending order to queue - OrderId: {order.OrderId}, TotalAmount: {order.TotalAmount}");
                Console.WriteLine($"Serialized order: {message}");
                await _orderQueueClient.SendMessageAsync(message);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending order message: {ex.Message}");
                return false;
            }
        }

        public async Task<Order?> ReceiveOrderMessageAsync()
        {
            try
            {
                var messages = await _orderQueueClient.ReceiveMessagesAsync(maxMessages: 1);
                
                if (messages.Value.Length == 0)
                    return null;

                var message = messages.Value[0];
                Console.WriteLine($"Received message from queue: {message.MessageText}");
                
                var order = JsonSerializer.Deserialize<Order>(message.MessageText);

                if (order != null)
                {
                    Console.WriteLine($"Deserialized order - OrderId: {order.OrderId}, TotalAmount: {order.TotalAmount}");
                    // Store the message ID and pop receipt for later deletion
                    order.Notes = $"MessageId:{message.MessageId},PopReceipt:{message.PopReceipt}";
                }

                return order;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error receiving order message: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> DeleteOrderMessageAsync(QueueMessage message)
        {
            try
            {
                await _orderQueueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting order message: {ex.Message}");
                return false;
            }
        }

        public async Task<int> GetOrderQueueLengthAsync()
        {
            try
            {
                var properties = await _orderQueueClient.GetPropertiesAsync();
                return properties.Value.ApproximateMessagesCount;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting order queue length: {ex.Message}");
                return -1;
            }
        }

        #endregion

        #region Inventory Queue Operations

        public async Task<bool> SendInventoryMessageAsync(string messageContent)
        {
            try
            {
                await _inventoryQueueClient.SendMessageAsync(messageContent);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending inventory message: {ex.Message}");
                return false;
            }
        }

        public async Task<string?> ReceiveInventoryMessageAsync()
        {
            try
            {
                var messages = await _inventoryQueueClient.ReceiveMessagesAsync(maxMessages: 1);
                
                if (messages.Value.Length == 0)
                    return null;

                var message = messages.Value[0];
                return message.MessageText;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error receiving inventory message: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> DeleteInventoryMessageAsync(QueueMessage message)
        {
            try
            {
                await _inventoryQueueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting inventory message: {ex.Message}");
                return false;
            }
        }

        public async Task<int> GetInventoryQueueLengthAsync()
        {
            try
            {
                var properties = await _inventoryQueueClient.GetPropertiesAsync();
                return properties.Value.ApproximateMessagesCount;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting inventory queue length: {ex.Message}");
                return -1;
            }
        }

        #endregion
    }
}
