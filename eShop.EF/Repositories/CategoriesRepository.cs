using eShop.Core.Models;
using eShop.Core.Services.Abstractions;
using eShop.Core.Services.Implementations;
using Microsoft.EntityFrameworkCore;

namespace eShop.EF.Repositories
{
    public class CategoriesRepository(ApplicationDbContext context) : BaseRepository<Category>(context), ICategoryRepository
    {
        private readonly ApplicationDbContext _context = context;
        public async Task<IEnumerable<Category>> GetCategoryTreeAsync()
        {
            return await _context.Categories
            .Where(c => c.ParentCategoryId == null)
            .Include(c => c.ChildCategories)
                .ThenInclude(c => c.ChildCategories)
                .ThenInclude(c => c.ChildCategories)
                .ThenInclude(c => c.ChildCategories) 
                .ThenInclude(c => c.ChildCategories) 
            .ToListAsync();
        }
    }
}
