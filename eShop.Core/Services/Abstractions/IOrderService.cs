using eShop.Core.DTOs.Orders;
using eShop.Core.Models;
using eShop.Core.Enums;

namespace eShop.Core.Services.Abstractions
{
    public interface IOrderService
    {
        Task<OrderDto> CreateOrderAsync(CreateOrderDto order);
        Task<Order?> GetOrderByIdAsync(int orderId);
        Task<IEnumerable<Order>> GetUserOrdersAsync(string userId);
        Task<IEnumerable<Order>> GetAllOrdersAsync(int page = 1, int pageSize = 10);
        Task<Order> UpdateOrderAsync(UpdateOrderDto updateOrderDto);
        Task<bool> DeleteOrderAsync(int orderId);
        Task<Order> UpdateOrderStatusAsync(int orderId, ShippingStatus? status, PaymentStatus? paymentStatus);
        Task<OrderDto> CancelOrderAsync(int orderId, string? cancellationReason = null);
    }
}
