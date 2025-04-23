using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SocialEngineeringPlatform.Web.Models.Enums; // 引用 Enums 命名空間

namespace SocialEngineeringPlatform.Web.Models.Core
{
    public class MailSendLog
    {
        [Key]
        public long Id { get; set; } // 使用 long (BIGINT)

        public int CampaignId { get; set; } // 外鍵
        public int TargetUserId { get; set; } // 外鍵

        public DateTime SendTime { get; set; } = DateTime.UtcNow;

        [Required]
        // [MaxLength(50)] // MaxLength 將由 Fluent API 設定
        public required CampaignSendStatus Status { get; set; } // *** 改用 Enum *** (與 CampaignTarget 狀態一致)

        [MaxLength(256)]
        public string? SmtpServerUsed { get; set; }

        public string? ErrorMessage { get; set; }

        // 導覽屬性
        [ForeignKey("CampaignId")]
        public virtual Campaign? Campaign { get; set; }

        [ForeignKey("TargetUserId")]
        public virtual TargetUser? TargetUser { get; set; }
    }
}