using System.ComponentModel.DataAnnotations;

namespace SocialEngineeringPlatform.Web.Models.Core
{
    public class MailTemplateCategory
    {
        
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "分類名稱為必填欄位。")]
        [MaxLength(100)]
        [Display(Name = "分類名稱")] // 設定顯示名稱
        public required string Name { get; set; }

        [MaxLength(500)]
        [Display(Name = "描述")] // 設定顯示名稱
        public string? Description { get; set; }

        // 導覽屬性: 此分類下的郵件範本
        // 注意：在 Index 頁面通常不需要載入此關聯資料，除非特別要顯示範本數量等
        [Display(Name = "郵件範本")]
        public virtual ICollection<MailTemplate> MailTemplates { get; set; } = new List<MailTemplate>();
    }
}