using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SocialEngineeringPlatform.Web.Models.Core
{
    public class MailTemplate
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "範本名稱為必填欄位。")]
        [MaxLength(256)]
        [Display(Name = "範本名稱")] // 中文標籤
        public required string Name { get; set; }

        [Required(ErrorMessage = "主旨為必填欄位。")]
        [MaxLength(256)]
        [Display(Name = "主旨")] // 中文標籤
        public required string Subject { get; set; }

        [Required(ErrorMessage = "內文為必填欄位。")]
        [Display(Name = "內文")] // 中文標籤
        public required string Body { get; set; } // 假設對應 NVARCHAR(MAX)

        [MaxLength(10)]
        [Display(Name = "語言")] // 中文標籤
        public string? Language { get; set; }

        [Display(Name = "所屬分類ID")] // 中文標籤 (ID)
        public int? CategoryId { get; set; } // 外鍵 (可為空)

        [MaxLength(256)]
        [Display(Name = "附件路徑")] // 中文標籤
        public string? AttachmentPath { get; set; }
        
        // *** 新增：自訂寄件者 Email (可選) ***
        [MaxLength(255)]
        [EmailAddress(ErrorMessage = "自訂寄件者 Email 格式不正確。")] // 加入驗證
        [Display(Name = "自訂寄件者 Email (可選)")]
        public string? CustomFromAddress { get; set; } // Nullable

        // *** 新增：自訂寄件者顯示名稱 (可選) ***
        [MaxLength(255)]
        [Display(Name = "自訂寄件者名稱 (可選)")]
        public string? CustomFromDisplayName { get; set; } // Nullable
        

        [Display(Name = "建立時間")] // 中文標籤
        public DateTime CreateTime { get; set; } = DateTime.UtcNow;

        [Display(Name = "更新時間")] // 中文標籤
        public DateTime UpdateTime { get; set; } = DateTime.UtcNow;

        [Required]
        [Display(Name = "建立者ID")] // 中文標籤 (ID)
        public required string CreatedByUserId { get; set; } // string key

        // 導覽屬性 (Navigation Properties)
        [ForeignKey("CategoryId")]
        [Display(Name = "所屬分類")] // 中文標籤 (關聯物件)
        public virtual MailTemplateCategory? Category { get; set; }

        [ForeignKey("CreatedByUserId")]
        [Display(Name = "建立者")] // 中文標籤 (關聯物件)
        public virtual ApplicationUser? CreatedByUser { get; set; }

        // 其他導覽屬性
        public virtual ICollection<Campaign> Campaigns { get; set; } = new List<Campaign>();
        public virtual ICollection<TrackingEvent> TrackingEvents { get; set; } = new List<TrackingEvent>();
    }
}