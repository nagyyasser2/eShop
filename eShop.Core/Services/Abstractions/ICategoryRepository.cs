using eShop.Core.Models;

namespace eShop.Core.Services.Abstractions
{
    public interface ICategoryRepository: IBaseRepository<Category>
    {
        //Task<ICategoryRepository> GetCategoryTreeAsync();
        public  Task<IEnumerable<Category>> GetCategoryTreeAsync();
    }
}
