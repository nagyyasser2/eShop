using AutoMapper;
using eShop.Core.DTOs.Orders;
using eShop.Core.Exceptions;
using eShop.Core.Models;
using eShop.Core.Services.Abstractions;
using eShop.Core.Services.Base;

namespace eShop.Core.Services.Implementations
{
    public class OrderItemService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IProductService productService,
        IVariantService variantService) : BaseOrderService(unitOfWork, variantService, productService, mapper), IOrderItemService
    {
        public async Task<OrderItem> CreateOrderItemAsync(CreateOrderItemDto createOrderItem)
        {
            return await ExecuteInTransactionAsync(async () =>
            {
                var order = await GetValidatedOrderAsync(createOrderItem.OrderId);
                var product = await GetValidatedProductAsync(createOrderItem.ProductId);

                await ValidateStockAvailability(createOrderItem.ProductId, createOrderItem.ProductVariantId, createOrderItem.Quantity);

                var orderItem = _mapper.Map<OrderItem>(createOrderItem);

                // Set product details and prices correctly
                orderItem.UnitPrice = product.Price;
                orderItem.TotalPrice = orderItem.Quantity * product.Price;
                orderItem.ProductName = product.Name;
                orderItem.ProductSKU = product.SKU;

                var createdOrderItem = await _unitOfWork.OrderItemRepository.AddAsync(orderItem);

                // Update stock
                await UpdateStockForQuantityChangeAsync(createOrderItem.ProductId, createOrderItem.ProductVariantId, createOrderItem.Quantity);

                await _unitOfWork.SaveChangesAsync();

                // Recalculate order totals after adding item
                await RecalculateOrderTotalsAsync(createOrderItem.OrderId);

                return createdOrderItem;
            });
        }

        public async Task<IEnumerable<OrderItem>> CreateOrderItemsAsync(IEnumerable<CreateOrderItemDto> createOrderItems)
        {
            return await ExecuteInTransactionAsync(async () =>
            {
                var createdItems = new List<OrderItem>();
                var productUpdates = new Dictionary<int, (int totalQuantity, Product product)>();
                int? orderId = null;

                // Validate all items first and gather product details
                foreach (var createOrderItem in createOrderItems)
                {
                    orderId = createOrderItem.OrderId;

                    var order = await GetValidatedOrderAsync(createOrderItem.OrderId);
                    var product = await GetValidatedProductAsync(createOrderItem.ProductId);

                    // Store product details for later use and track quantities
                    if (productUpdates.ContainsKey(product.Id))
                    {
                        productUpdates[product.Id] = (
                            productUpdates[product.Id].totalQuantity + createOrderItem.Quantity,
                            product
                        );
                    }
                    else
                    {
                        productUpdates[product.Id] = (createOrderItem.Quantity, product);
                    }

                    // Check if we have enough stock for total quantity
                    await ValidateStockAvailability(product.Id, createOrderItem.ProductVariantId, productUpdates[product.Id].totalQuantity);
                }

                // Create order items with correct pricing
                foreach (var createOrderItem in createOrderItems)
                {
                    var orderItem = _mapper.Map<OrderItem>(createOrderItem);
                    var product = productUpdates[createOrderItem.ProductId].product;

                    // Set correct pricing and product details
                    orderItem.UnitPrice = product.Price;
                    orderItem.TotalPrice = orderItem.Quantity * product.Price;
                    orderItem.ProductName = product.Name;
                    orderItem.ProductSKU = product.SKU;

                    createdItems.Add(orderItem);
                }

                // Add all items at once
                await _unitOfWork.OrderItemRepository.AddRangeAsync(createdItems);

                // Update product stock quantities
                foreach (var productUpdate in productUpdates)
                {
                    await UpdateStockForQuantityChangeAsync(productUpdate.Key, null, productUpdate.Value.totalQuantity);
                }

                await _unitOfWork.SaveChangesAsync();

                // Recalculate order totals after adding items
                if (orderId.HasValue)
                {
                    await RecalculateOrderTotalsAsync(orderId.Value);
                }

                return createdItems;
            });
        }

        public async Task<IEnumerable<OrderItem>> CreateOrderItemsWithoutTransactionAsync(IEnumerable<CreateOrderItemDto> createOrderItems)
        {
            var createdItems = new List<OrderItem>();

            foreach (var createOrderItem in createOrderItems)
            {
                var orderItem = _mapper.Map<OrderItem>(createOrderItem);
                orderItem.TotalPrice = orderItem.Quantity * orderItem.UnitPrice;
                createdItems.Add(orderItem);
            }

            await _unitOfWork.OrderItemRepository.AddRangeAsync(createdItems);
            return createdItems;
        }

        public async Task<OrderItem?> GetOrderItemByIdAsync(int id)
        {
            return await _unitOfWork.OrderItemRepository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<OrderItem>> GetOrderItemsByOrderIdAsync(int orderId)
        {
            var includes = GetOrderItemIncludes(false, false, false);
            return await _unitOfWork.OrderItemRepository.FindAllAsync(oi => oi.OrderId == orderId, includes)
                   ?? Enumerable.Empty<OrderItem>();
        }

        public async Task<OrderItem> UpdateOrderItemQuantityAsync(UpdateOrderItemDto updateOrderItem)
        {
            return await ExecuteInTransactionAsync(async () =>
            {
                var existingOrderItem = await _unitOfWork.OrderItemRepository.GetByIdAsync(updateOrderItem.Id);
                if (existingOrderItem == null)
                    throw new NotFoundException($"OrderItem with Id: {updateOrderItem.Id} not found.");

                var quantityDifference = updateOrderItem.Quantity - existingOrderItem.Quantity;

                // Update stock based on quantity difference
                if (quantityDifference != 0)
                {
                    await UpdateStockForQuantityChangeAsync(
                        existingOrderItem.ProductId,
                        existingOrderItem.ProductVariantId,
                        quantityDifference);
                }

                existingOrderItem.Quantity = updateOrderItem.Quantity;
                existingOrderItem.TotalPrice = existingOrderItem.Quantity * existingOrderItem.UnitPrice;

                await _unitOfWork.SaveChangesAsync();
                await RecalculateOrderTotalsAsync(existingOrderItem.OrderId);

                return existingOrderItem;
            });
        }

        public async Task<bool> DeleteOrderItemAsync(int id)
        {
            return await ExecuteInTransactionAsync(async () =>
            {
                var existingItem = await _unitOfWork.OrderItemRepository.GetByIdAsync(id);
                if (existingItem == null)
                    throw new NotFoundException($"OrderItem with Id: {id}, NotFound!.");

                var orderId = existingItem.OrderId;

                // Restore stock (negative quantity difference means restore)
                await UpdateStockForQuantityChangeAsync(
                    existingItem.ProductId,
                    existingItem.ProductVariantId,
                    -existingItem.Quantity);

                await _unitOfWork.OrderItemRepository.RemoveByIdAsync(id);
                await _unitOfWork.SaveChangesAsync();
                await RecalculateOrderTotalsAsync(orderId);

                return true;
            });
        }
    }
}