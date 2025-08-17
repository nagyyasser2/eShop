using eShop.Core.Models;

namespace eShop.Core.Services.Abstractions
{
    public interface IOrderItemService
    {
        Task<OrderItem> CreateOrderItemAsync(OrderItem orderItem);
        Task<IEnumerable<OrderItem>> CreateOrderItemsAsync(IEnumerable<OrderItem> orderItems);
        Task<OrderItem?> GetOrderItemByIdAsync(int id, bool includeOrder = false, bool includeProduct = false, bool includeVariant = false);
        Task<IEnumerable<OrderItem>> GetOrderItemsByOrderIdAsync(int orderId, bool includeProduct = false, bool includeVariant = false);
        Task<OrderItem> UpdateOrderItemAsync(OrderItem orderItem);
        Task<bool> DeleteOrderItemAsync(int id);
    }
}
