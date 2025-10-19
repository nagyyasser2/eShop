namespace eShop.Core.DTOs.Products
{
    public class UpdateProductRequest: ProductDto
    {
        public ICollection<UpdateProductImageRequest> ProductImages { get; set; }
    }
}
