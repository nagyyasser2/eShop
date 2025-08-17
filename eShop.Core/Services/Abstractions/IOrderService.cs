using eShop.Core.DTOs;
using eShop.Core.Models;
using eShopApi.Core.Enums;

namespace eShop.Core.Services.Abstractions
{
    public interface IOrderService
    {
        Task<OrderDto> CreateOrderAsync(CreateOrderDto order, string userId);
        Task<Order?> GetOrderByIdAsync(int orderId, bool includeItems = false, bool includePayments = false);
        Task<IEnumerable<Order>> GetOrdersByUserIdAsync(string userId, bool includeItems = false, bool includePayments = false);
        Task<IEnumerable<Order>> GetAllOrdersAsync(bool includeItems = false, bool includePayments = false);
        Task<Order> UpdateOrderStatusAsync(int orderId, OrderStatus status);
        Task<Order> UpdateOrderAsync(Order order);
        Task<bool> DeleteOrderAsync(int orderId);
    }
}
