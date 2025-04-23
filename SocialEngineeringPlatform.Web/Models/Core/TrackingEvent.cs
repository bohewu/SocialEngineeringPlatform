using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SocialEngineeringPlatform.Web.Models.Enums; // 引用 Enums 命名空間

namespace SocialEngineeringPlatform.Web.Models.Core
{
    public class TrackingEvent
    {
        [Key]
        public long Id { get; set; } // 使用 long (BIGINT) 以應付大量事件

        public int CampaignId { get; set; } // 外鍵
        public int TargetUserId { get; set; } // 外鍵
        public int? MailTemplateId { get; set; } // 外鍵 (可為空，記錄當時範本)
        public int? LandingPageTemplateId { get; set; } // 外鍵 (可為空，記錄當時登陸頁)


        public DateTime EventTime { get; set; } = DateTime.UtcNow;

        [Required]
        // [MaxLength(50)] // MaxLength 將由 Fluent API 設定
        public required TrackingEventType EventType { get; set; } // *** 改用 Enum ***

        public string? EventDetails { get; set; } // 例如: Clicked URL, User Agent, IP Address (注意隱私)

        // 導覽屬性
        [ForeignKey("CampaignId")]
        public virtual Campaign? Campaign { get; set; }

        [ForeignKey("TargetUserId")]
        public virtual TargetUser? TargetUser { get; set; }

        [ForeignKey("MailTemplateId")]
        public virtual MailTemplate? MailTemplate { get; set; }

        [ForeignKey("LandingPageTemplateId")]
        public virtual LandingPageTemplate? LandingPageTemplate { get; set; }
    }
}