using eShop.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eShop.EF.Configurations
{
    public class ProductImageEntityTypeConfiguration: IEntityTypeConfiguration<Image>
    {
        public void Configure(EntityTypeBuilder<Image> builder)
        {
            //builder.HasKey(pi => new { pi.ProductId, pi.ImageId });
        }
    }
}
