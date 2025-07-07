using eShopApi.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace eShop.Core.Models
{
    public class Setting
    {
        public int Id { get; set; }
        [Required, MaxLength(100)]
        public string Key { get; set; }
        public string? Value { get; set; }
        public string? Description { get; set; }
        public SettingType Type { get; set; } = SettingType.String;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
