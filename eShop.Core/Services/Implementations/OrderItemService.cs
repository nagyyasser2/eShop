using eShop.Core.Models;
using eShop.Core.Services.Abstractions;

namespace eShop.Core.Services.Implementations
{
    public class OrderItemService : IOrderItemService
    {
        private readonly IUnitOfWork _unitOfWork;

        public OrderItemService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<OrderItem> CreateOrderItemAsync(OrderItem orderItem)
        {
            await _unitOfWork.OrderItemRepository.AddAsync(orderItem);
            await _unitOfWork.SaveChangesAsync();
            return orderItem;
        }

        public async Task<IEnumerable<OrderItem>> CreateOrderItemsAsync(IEnumerable<OrderItem> orderItems)
        {
            await _unitOfWork.OrderItemRepository.AddRangeAsync(orderItems);
            await _unitOfWork.SaveChangesAsync();
            return orderItems;
        }

        public async Task<OrderItem?> GetOrderItemByIdAsync(int id, bool includeOrder = false, bool includeProduct = false, bool includeVariant = false)
        {
            var includes = GetIncludes(includeOrder, includeProduct, includeVariant);
            return await _unitOfWork.OrderItemRepository.GetByIdAsync(id, includes);
        }

        public async Task<IEnumerable<OrderItem>> GetOrderItemsByOrderIdAsync(int orderId, bool includeProduct = false, bool includeVariant = false)
        {
            var includes = GetIncludes(false, includeProduct, includeVariant);
            return await _unitOfWork.OrderItemRepository.FindAllAsync(oi => oi.OrderId == orderId, includes)
                   ?? Enumerable.Empty<OrderItem>();
        }

        public async Task<OrderItem> UpdateOrderItemAsync(OrderItem orderItem)
        {
            _unitOfWork.OrderItemRepository.Update(orderItem);
            await _unitOfWork.SaveChangesAsync();
            return orderItem;
        }

        public async Task<bool> DeleteOrderItemAsync(int id)
        {
            var existingItem = await _unitOfWork.OrderItemRepository.GetByIdAsync(id);
            if (existingItem == null)
                return false;

            await _unitOfWork.OrderItemRepository.RemoveByIdAsync(id);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Builds includes array dynamically based on parameters.
        /// </summary>
        private string[]? GetIncludes(bool includeOrder, bool includeProduct, bool includeVariant)
        {
            var includes = new List<string>();
            if (includeOrder) includes.Add(nameof(OrderItem.Order));
            if (includeProduct) includes.Add(nameof(OrderItem.Product));
            if (includeVariant) includes.Add(nameof(OrderItem.ProductVariant));
            return includes.Count > 0 ? includes.ToArray() : null;
        }
    }
}
