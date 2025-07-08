using eShop.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eShop.EF.Configurations
{
    public class ProductImageEntityTypeConfiguration: IEntityTypeConfiguration<ProductImage>
    {
        public void Configure(EntityTypeBuilder<ProductImage> builder)
        {
            //builder.HasKey(pi => new { pi.ProductId, pi.ImageId });
        }
    }
}
