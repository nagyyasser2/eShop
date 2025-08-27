using eShop.Core.DTOs.Orders;
using eShop.Core.Models;

namespace eShop.Core.Services.Abstractions
{
    public interface IOrderItemService
    {
        Task<OrderItem> CreateOrderItemAsync(CreateOrderItemDto orderItem);
        Task<IEnumerable<OrderItem>> CreateOrderItemsAsync(IEnumerable<CreateOrderItemDto> orderItems);
        Task<OrderItem?> GetOrderItemByIdAsync(int id);
        Task<IEnumerable<OrderItem>> GetOrderItemsByOrderIdAsync(int orderId);
        Task<OrderItem> UpdateOrderItemQuantityAsync(UpdateOrderItemDto orderItem);
        Task<bool> DeleteOrderItemAsync(int id);
    }
}
