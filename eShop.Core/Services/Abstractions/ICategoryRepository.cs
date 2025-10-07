using eShop.Core.Models;

namespace eShop.Core.Services.Abstractions
{
    public interface ICategoryRepository: IBaseRepository<Category>
    {
        public  Task<IEnumerable<Category>> GetCategoryTreeAsync();
    }
}
