using eShop.Core.Services.Abstractions;
using eShop.Core.Services.Base;
using eShop.Core.DTOs.Orders;
using eShop.Core.Models;
using eShop.Core.Enums;
using AutoMapper;

namespace eShop.Core.Services.Implementations
{
    public class OrderService(
        IUnitOfWork unitOfWork,
        IVariantService variantService,
        IProductService productService,
        IMapper mapper) : BaseOrderService(unitOfWork, variantService, productService, mapper), IOrderService
    {
        public async Task<OrderDto> CreateOrderAsync(CreateOrderDto orderDto)
        {
            await ValidateOrderItemsAsync(orderDto);

            return await ExecuteInTransactionAsync(async () =>
            {
                var order = await CreateOrderEntityAsync(orderDto);

                await CreateOrderItemsDirectAsync(order, orderDto.OrderItems);

                // 3. Update stock quantities
                await UpdateStockQuantitiesAsync(orderDto.OrderItems);

                // 4. Calculate and save the totals
                await UpdateOrderTotalsDirectAsync(order);

                // 5. Return the complete order with calculated totals
                var orderWithItems = await GetOrderByIdAsync(order.Id);
                return _mapper.Map<OrderDto>(orderWithItems);
            });
        }

        public async Task<OrderDto> CancelOrderAsync(int orderId, string? cancellationReason = null)
        {
            return await ExecuteInTransactionAsync(async () =>
            {
                var order = await GetValidatedOrderAsync(orderId, new[] { nameof(Order.OrderItems) });

                // Check if order can be cancelled
                if (order.ShippingStatus == ShippingStatus.Shipped ||
                    order.ShippingStatus == ShippingStatus.Delivered ||
                    order.ShippingStatus == ShippingStatus.Cancelled)
                {
                    throw new InvalidOperationException($"Cannot cancel order with status: {order.ShippingStatus}");
                }

                // Update order status
                order.ShippingStatus = ShippingStatus.Cancelled;
                order.PaymentStatus = PaymentStatus.Cancelled;
                order.UpdatedAt = DateTime.UtcNow;
                order.Notes = string.IsNullOrEmpty(order.Notes)
                    ? $"Cancelled: {cancellationReason}"
                    : $"{order.Notes}\nCancelled: {cancellationReason}";

                // Restore stock quantities
                await RestoreStockQuantitiesAsync(order.OrderItems);

                _unitOfWork.OrderRepository.Update(order);
                await _unitOfWork.SaveChangesAsync();

                return _mapper.Map<OrderDto>(order);
            });
        }

        public async Task<Order?> GetOrderByIdAsync(int orderId)
        {
            var includes = GetOrderIncludes(true, true, true);
            return await _unitOfWork.OrderRepository.GetByIdAsync(orderId, includes);
        }

        public async Task<IEnumerable<Order>> GetUserOrdersAsync(string userId)
        {
            return await _unitOfWork.OrderRepository.FindAllAsync(o => o.UserId == userId, ["OrderItems"])
                   ?? Enumerable.Empty<Order>();
        }

        public async Task<IEnumerable<Order>> GetAllOrdersAsync(int page = 1, int size = 10)
        {
            return await _unitOfWork.OrderRepository.GetAllPagedAsync(page, size, ["User", "OrderItems"]);
        }

        public async Task<Order> UpdateOrderStatusAsync(int orderId, ShippingStatus? shippingStatus, PaymentStatus? paymentStatus)
        {
            var order = await GetValidatedOrderAsync(orderId);

            if (shippingStatus.HasValue)
                order.ShippingStatus = shippingStatus.Value;

            if (paymentStatus.HasValue)
                order.PaymentStatus = paymentStatus.Value;

            order.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.OrderRepository.Update(order);
            await _unitOfWork.SaveChangesAsync();
            return order;
        }

        public async Task<Order> UpdateOrderAsync(UpdateOrderDto updateOrderDto)
        {
            var order = _mapper.Map<Order>(updateOrderDto);
            _unitOfWork.OrderRepository.Update(order);
            await _unitOfWork.SaveChangesAsync();
            return order;
        }

        public async Task<bool> DeleteOrderAsync(int orderId)
        {
            return await ExecuteInTransactionAsync(async () =>
            {
                var order = await GetValidatedOrderAsync(orderId, new[] { nameof(Order.OrderItems) });

                await RestoreStockQuantitiesAsync(order.OrderItems);

                await _unitOfWork.OrderRepository.RemoveByIdAsync(orderId);
                await _unitOfWork.SaveChangesAsync();
                return true;
            });
        }

        private async Task<Order> CreateOrderEntityAsync(CreateOrderDto orderDto)
        {
            var order = _mapper.Map<Order>(orderDto);

            // Set initial values - totals will be calculated later
            order.OrderNumber = GenerateOrderNumber();
            order.SubTotal = 0; // Will be calculated after items are added
            order.TotalAmount = 0; // Will be calculated after items are added
            order.CreatedAt = DateTime.UtcNow;
            order.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.OrderRepository.AddAsync(order);
            await _unitOfWork.SaveChangesAsync();

            return order;
        }

        private async Task CreateOrderItemsDirectAsync(Order order, List<CreateOrderItemDto> orderItems)
        {
            var orderItemEntities = new List<OrderItem>();

            foreach (var item in orderItems)
            {
                // Validate product exists
                var product = await GetValidatedProductAsync(item.ProductId);

                // Create order item entity
                var orderItem = _mapper.Map<OrderItem>(item);

                orderItem.OrderId = order.Id;
                orderItem.UnitPrice = product.Price;
                orderItem.TotalPrice = orderItem.Quantity * product.Price;
                orderItem.ProductName = product.Name;
                orderItem.ProductSku = product.Sku;

                orderItemEntities.Add(orderItem);
            }

            // Add all order items at once
            await _unitOfWork.OrderItemRepository.AddRangeAsync(orderItemEntities);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}