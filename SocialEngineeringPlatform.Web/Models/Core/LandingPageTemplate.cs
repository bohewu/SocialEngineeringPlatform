using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SocialEngineeringPlatform.Web.Models.Core
{
    public class LandingPageTemplate
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "範本名稱為必填欄位。")]
        [MaxLength(256)]
        [Display(Name = "範本名稱")] // 中文標籤
        public required string Name { get; set; }

        [Required(ErrorMessage = "HTML 內容為必填欄位。")]
        [Display(Name = "HTML 內容")] // 中文標籤
        public required string HtmlContent { get; set; } // 假設對應 NVARCHAR(MAX)

        [Display(Name = "收集欄位設定")] // 中文標籤 (JSON or similar)
        public string? CollectFieldsConfig { get; set; }

        [Display(Name = "建立時間")] // 中文標籤
        public DateTime CreateTime { get; set; } = DateTime.UtcNow;

        [Display(Name = "更新時間")] // 中文標籤
        public DateTime UpdateTime { get; set; } = DateTime.UtcNow;

        [Required]
        [Display(Name = "建立者ID")] // 中文標籤 (ID)
        public required string CreatedByUserId { get; set; } // string key

        // 導覽屬性 (Navigation Properties)
        [ForeignKey("CreatedByUserId")]
        [Display(Name = "建立者")] // 中文標籤 (關聯物件)
        public virtual ApplicationUser? CreatedByUser { get; set; }

        // 其他導覽屬性
        public virtual ICollection<Campaign> Campaigns { get; set; } = new List<Campaign>();
        public virtual ICollection<TrackingEvent> TrackingEvents { get; set; } = new List<TrackingEvent>();
    }
}