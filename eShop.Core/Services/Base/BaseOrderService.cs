using eShop.Core.Services.Abstractions;
using eShop.Core.Exceptions;
using eShop.Core.DTOs.Orders;
using eShop.Core.Models;
using AutoMapper;

namespace eShop.Core.Services.Base
{
    public abstract class BaseOrderService(
        IUnitOfWork unitOfWork,
        IVariantService variantService,
        IProductService productService,
        IMapper mapper)
    {
        protected readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        protected readonly IVariantService _variantService = variantService ?? throw new ArgumentNullException(nameof(variantService));
        protected readonly IProductService _productService = productService ?? throw new ArgumentNullException(nameof(productService));
        protected readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

        #region Stock Management

        protected async Task UpdateStockQuantitiesAsync(IEnumerable<CreateOrderItemDto> orderItems)
        {
            foreach (var item in orderItems)
            {
                await UpdateSingleItemStockAsync(item.ProductId, item.ProductVariantId, item.Quantity, isReduction: true);
            }
        }

        protected async Task RestoreStockQuantitiesAsync(ICollection<OrderItem> orderItems)
        {
            foreach (var item in orderItems)
            {
                await UpdateSingleItemStockAsync(item.ProductId, item.ProductVariantId, item.Quantity, isReduction: false);
            }
        }

        protected async Task UpdateStockForQuantityChangeAsync(int productId, int? variantId, int quantityDifference)
        {
            if (quantityDifference == 0) return;

            bool isReduction = quantityDifference > 0;
            int absoluteQuantity = Math.Abs(quantityDifference);

            await UpdateSingleItemStockAsync(productId, variantId, absoluteQuantity, isReduction);
        }

        private async Task UpdateSingleItemStockAsync(int productId, int? variantId, int quantity, bool isReduction)
        {
            if (variantId.HasValue)
            {
                var variantDto = await _variantService.GetVariantByIdAsync(variantId.Value);
                if (variantDto == null)
                    throw new NotFoundException($"Variant with ID {variantId.Value} not found.");

                int newQuantity = isReduction
                    ? variantDto.StockQuantity - quantity
                    : variantDto.StockQuantity + quantity;

                await _variantService.UpdateStockQuantityAsync(variantId.Value, newQuantity);
            }
            else
            {
                var productDto = await _productService.GetProductByIdAsync(productId);
                if (productDto == null)
                    throw new NotFoundException($"Product with ID {productId} not found.");

                int newQuantity = isReduction
                    ? productDto.StockQuantity - quantity
                    : productDto.StockQuantity + quantity;

                await _productService.UpdateStockQuantityAsync(productId, newQuantity);
            }
        }

        #endregion

        #region Order Totals Calculation

        protected async Task RecalculateOrderTotalsAsync(int orderId)
        {
            var order = await _unitOfWork.OrderRepository.GetByIdAsync(orderId, new[] { nameof(Order.OrderItems) });
            if (order == null)
                throw new NotFoundException($"Order with ID {orderId} not found.");

            await UpdateOrderTotalsDirectAsync(order);
        }

        protected async Task UpdateOrderTotalsDirectAsync(Order order)
        {
            // Always reload the order with items to ensure we have the latest data
            var orderWithItems = await _unitOfWork.OrderRepository.GetByIdAsync(order.Id, new[] { nameof(Order.OrderItems) });

            if (orderWithItems == null)
                throw new NotFoundException($"Order with ID {order.Id} not found.");

            // Calculate SubTotal from OrderItems
            order.SubTotal = orderWithItems.OrderItems?.Sum(item => item.TotalPrice) ?? 0;

            // Calculate TotalAmount = SubTotal + TaxAmount + ShippingAmount - DiscountAmount
            order.TotalAmount = order.SubTotal + order.TaxAmount + order.ShippingAmount - order.DiscountAmount;

            // Update timestamp
            order.UpdatedAt = DateTime.UtcNow;

            // Update the order
            _unitOfWork.OrderRepository.Update(order);

            // Save the changes immediately
            await _unitOfWork.SaveChangesAsync();
        }

        #endregion

        #region Validation

        protected async Task ValidateOrderItemAsync(CreateOrderItemDto item)
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

            // Calculate total price for the order item
            item.TotalPrice = item.Quantity * item.UnitPrice;
        }

        protected async Task ValidateOrderItemsAsync(CreateOrderDto orderDto)
        {
            if (orderDto.OrderItems == null || !orderDto.OrderItems.Any())
                throw new ArgumentException("At least one order item is required.");

            foreach (var item in orderDto.OrderItems)
            {
                await ValidateOrderItemAsync(item);
            }
        }

        protected async Task ValidateStockAvailability(int productId, int? variantId, int requestedQuantity)
        {
            if (variantId.HasValue)
            {
                var variant = await _variantService.GetVariantByIdAsync(variantId.Value);
                if (variant == null)
                    throw new NotFoundException($"Variant with ID {variantId.Value} not found.");

                if (variant.StockQuantity < requestedQuantity)
                    throw new InsufficientStockException($"Insufficient stock for variant {variant.SKU}. Available: {variant.StockQuantity}, Requested: {requestedQuantity}");
            }
            else
            {
                var product = await _productService.GetProductByIdAsync(productId);
                if (product == null)
                    throw new NotFoundException($"Product with ID {productId} not found.");

                if (product.StockQuantity < requestedQuantity)
                    throw new InsufficientStockException($"Insufficient stock for product {product.Name}. Available: {product.StockQuantity}, Requested: {requestedQuantity}");
            }
        }

        #endregion

        #region Transaction Helpers
        protected async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation)
        {
            await using var transaction = _unitOfWork.BeginTransaction();
            try
            {
                var result = await operation();
                await transaction.CommitAsync();
                return result;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        #endregion

        #region Include Helpers

        protected string[]? GetOrderIncludes(bool includeItems = false, bool includePayments = false, bool includeUser = false)
        {
            var includes = new List<string>();

            if (includeItems) includes.Add(nameof(Order.OrderItems));
            if (includePayments) includes.Add(nameof(Order.Payments));
            if (includeUser) includes.Add(nameof(Order.User));

            return includes.Count > 0 ? includes.ToArray() : null;
        }

        protected string[]? GetOrderItemIncludes(bool includeOrder = false, bool includeProduct = false, bool includeVariant = false)
        {
            var includes = new List<string>();

            if (includeOrder) includes.Add(nameof(OrderItem.Order));
            if (includeProduct) includes.Add(nameof(OrderItem.Product));
            if (includeVariant) includes.Add(nameof(OrderItem.ProductVariant));

            return includes.Count > 0 ? includes.ToArray() : null;
        }

        #endregion

        #region Utility Methods

        protected string GenerateOrderNumber()
        {
            return "ORD" + Guid.NewGuid().ToString("N").Substring(0, 7).ToUpper();
        }

        protected async Task<Product> GetValidatedProductAsync(int productId)
        {
            var product = await _unitOfWork.ProductRepository.GetByIdAsync(productId);
            if (product == null)
                throw new NotFoundException($"Product with ID {productId} not found.");
            return product;
        }

        protected async Task<Order> GetValidatedOrderAsync(int orderId, string[]? includes = null)
        {
            var order = await _unitOfWork.OrderRepository.GetByIdAsync(orderId, includes);
            if (order == null)
                throw new NotFoundException($"Order with ID {orderId} not found.");
            return order;
        }

        #endregion
    }
}