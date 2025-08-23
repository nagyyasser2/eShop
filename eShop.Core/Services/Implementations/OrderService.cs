using AutoMapper;
using eShop.Core.DTOs;
using eShop.Core.Models;
using eShop.Core.Services.Abstractions;
using eShop.Core.Enums;

namespace eShop.Core.Services.Implementations
{
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IVariantService _variantService;
        private readonly IProductService _productService;
        private readonly IOrderItemService _orderItemService;
        private readonly IMapper _mapper;

        public OrderService(IUnitOfWork unitOfWork, IVariantService variantService, IProductService productService, IOrderItemService orderItemService, IMapper mapper)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _variantService = variantService ?? throw new ArgumentNullException(nameof(variantService));
            _productService = productService ?? throw new ArgumentNullException(nameof(productService));
            _orderItemService = orderItemService ?? throw new ArgumentNullException(nameof(orderItemService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public async Task<OrderDto> CreateOrderAsync(CreateOrderDto orderDto, string userId)
        {
            await ValidateOrderItemsAsync(orderDto);
            CalculateOrderTotals(orderDto);

            await using var transaction = _unitOfWork.BeginTransaction();

            try
            {
                var order = await CreateOrderEntityAsync(orderDto, userId);
                
                await CreateOrderItemsAsync(order, orderDto.OrderItems);
                await UpdateStockQuantitiesAsync(orderDto.OrderItems);
                await transaction.CommitAsync();

                return _mapper.Map<OrderDto>(order);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<OrderDto> CancelOrderAsync(int orderId, string? cancellationReason = null)
        {
            await using var transaction = _unitOfWork.BeginTransaction();

            try
            {
                var order = await _unitOfWork.OrderRepository.GetByIdAsync(orderId, new[] { nameof(Order.OrderItems) });
                if (order == null)
                    throw new KeyNotFoundException($"Order with ID {orderId} not found.");

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
                await transaction.CommitAsync();

                return _mapper.Map<OrderDto>(order);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task RestoreStockQuantitiesAsync(ICollection<OrderItem> orderItems)
        {
            foreach (var item in orderItems)
            {
                if (item.ProductVariantId.HasValue)
                {
                    var variantDto = await _variantService.GetVariantByIdAsync(item.ProductVariantId.Value);
                    if (variantDto != null)
                    {
                        await _variantService.UpdateStockQuantityAsync(item.ProductVariantId.Value,
                            variantDto.StockQuantity + item.Quantity);
                    }
                }
                else
                {
                    var productDto = await _productService.GetProductByIdAsync(item.ProductId);
                    if (productDto != null)
                    {
                        await _productService.UpdateStockQuantityAsync(item.ProductId,
                            productDto.StockQuantity + item.Quantity);
                    }
                }
            }
        }
        
        private async Task ValidateOrderItemsAsync(CreateOrderDto orderDto)
        {
            if (orderDto.OrderItems == null || !orderDto.OrderItems.Any())
                throw new ArgumentException("At least one order item is required.");

            foreach (var item in orderDto.OrderItems)
            {
                if (item.Quantity <= 0)
                    throw new ArgumentException($"Invalid quantity for product {item.ProductName}. Quantity must be greater than 0.");

                var productDto = await _productService.GetProductByIdAsync(item.ProductId);
                if (productDto == null)
                    throw new ArgumentException($"Product with ID {item.ProductId} does not exist.");

                if (item.ProductVariantId.HasValue)
                {
                    var variantDto = await _variantService.GetVariantByIdAsync(item.ProductVariantId.Value);
                    if (variantDto == null)
                        throw new ArgumentException($"Variant with ID {item.ProductVariantId} does not exist.");

                    if (variantDto.StockQuantity < item.Quantity)
                        throw new ArgumentException($"Insufficient stock for variant {variantDto.SKU}. Available: {variantDto.StockQuantity}, Requested: {item.Quantity}.");

                    if (variantDto.Price.HasValue && item.UnitPrice != variantDto.Price.Value)
                        throw new ArgumentException($"Unit price for variant {variantDto.SKU} does not match. Expected: {variantDto.Price.Value}, Provided: {item.UnitPrice}.");
                }
                else
                {
                    if (productDto.StockQuantity < item.Quantity)
                        throw new ArgumentException($"Insufficient stock for product {productDto.Name}. Available: {productDto.StockQuantity}, Requested: {item.Quantity}.");
                }

                item.TotalPrice = item.Quantity * productDto.Price;
            }
        }

        private void CalculateOrderTotals(CreateOrderDto orderDto)
        {
            orderDto.SubTotal = orderDto.OrderItems.Sum(item => item.TotalPrice);
            orderDto.TotalAmount = orderDto.SubTotal + orderDto.TaxAmount + orderDto.ShippingAmount - orderDto.DiscountAmount;
        }

        private async Task<Order> CreateOrderEntityAsync(CreateOrderDto orderDto, string userId)
        {
            var order = _mapper.Map<Order>(orderDto);
            order.OrderNumber = GenerateOrderNumber();
            order.UserId = userId;
            await _unitOfWork.OrderRepository.AddAsync(order);
            await _unitOfWork.SaveChangesAsync();
            return order;
        }

        private async Task CreateOrderItemsAsync(Order order, List<CreateOrderItemDto> orderItems)
        {
            foreach (var item in orderItems)
            {
                var orderItem = _mapper.Map<OrderItem>(item);
                orderItem.OrderId = order.Id;
                await _orderItemService.CreateOrderItemAsync(orderItem);
            }
        }

        private async Task UpdateStockQuantitiesAsync(List<CreateOrderItemDto> orderItems)
        {
            foreach (var item in orderItems)
            {
                if (item.ProductVariantId.HasValue)
                {
                    var variantDto = await _variantService.GetVariantByIdAsync(item.ProductVariantId.Value);
                    await _variantService.UpdateStockQuantityAsync(item.ProductVariantId.Value, variantDto.StockQuantity - item.Quantity);
                }
                else
                {
                    var productDto = await _productService.GetProductByIdAsync(item.ProductId);
                    await _productService.UpdateStockQuantityAsync(item.ProductId, productDto.StockQuantity - item.Quantity);
                }
            }
        }
        
        private string GenerateOrderNumber()
        {
            // Example: 10-digit alphanumeric code
            // "ORD" + 7 random alphanumeric chars = 10 chars total
            return "ORD" + Guid.NewGuid().ToString("N").Substring(0, 7).ToUpper();
        }
        
        public async Task<Order?> GetOrderByIdAsync(int orderId, bool includeItems = false, bool includePayments = false)
        {
            var includes = GetIncludes(includeItems, includePayments);
            return await _unitOfWork.OrderRepository.GetByIdAsync(orderId, includes);
        }

        public async Task<IEnumerable<Order>> GetOrdersByUserIdAsync(string userId, bool includeItems = false, bool includePayments = false)
        {
            var includes = GetIncludes(includeItems, includePayments);
            return await _unitOfWork.OrderRepository.FindAllAsync(o => o.UserId == userId, includes)
                   ?? Enumerable.Empty<Order>();
        }

        public async Task<IEnumerable<Order>> GetAllOrdersAsync(bool includeItems = false, bool includePayments = false)
        {
            var includes = GetIncludes(includeItems, includePayments);
            return await _unitOfWork.OrderRepository.GetAllAsync(includes);
        }

        public async Task<Order> UpdateOrderStatusAsync(int orderId, ShippingStatus status)
        {
            var order = await _unitOfWork.OrderRepository.GetByIdAsync(orderId);
            if (order == null)
                throw new KeyNotFoundException($"Order with ID {orderId} not found.");

            order.ShippingStatus = status;
            order.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.OrderRepository.Update(order);
            await _unitOfWork.SaveChangesAsync();


            return order;
        }

        public async Task<Order> UpdateOrderAsync(Order order)
        {
            _unitOfWork.OrderRepository.Update(order);
            await _unitOfWork.SaveChangesAsync();
            return order;
        }

        public async Task<bool> DeleteOrderAsync(int orderId)
        {
            var order = await _unitOfWork.OrderRepository.GetByIdAsync(orderId);
            if (order == null)
                return false;

            await _unitOfWork.OrderRepository.RemoveByIdAsync(orderId);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        private string[]? GetIncludes(bool includeItems, bool includePayments)
        {
            var includes = new List<string>();
            if (includeItems) includes.Add(nameof(Order.OrderItems));
            if (includePayments) includes.Add(nameof(Order.Payments));
            return includes.Count > 0 ? includes.ToArray() : null;
        }
    }
}
