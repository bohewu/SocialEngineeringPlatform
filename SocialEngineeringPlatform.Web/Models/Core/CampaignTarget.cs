using System.ComponentModel.DataAnnotations.Schema;
using SocialEngineeringPlatform.Web.Models.Enums;

namespace SocialEngineeringPlatform.Web.Models.Core
{
    // 代表 Campaign 與 TargetUser 的多對多關聯
    public class CampaignTarget
    {
        // 複合主鍵 (在 DbContext 中用 Fluent API 設定)
        public int CampaignId { get; set; }
        public int TargetUserId { get; set; }

        // [MaxLength(50)] // MaxLength 將由 Fluent API 設定
        public CampaignSendStatus SendStatus { get; set; } = CampaignSendStatus.Pending; // *** 改用 Enum 並設定預設值 ***

        public DateTime? SendTime { get; set; }
        public string? ErrorMessage { get; set; }

        // 導覽屬性
        [ForeignKey("CampaignId")]
        public virtual Campaign? Campaign { get; set; }

        [ForeignKey("TargetUserId")]
        public virtual TargetUser? TargetUser { get; set; }
    }
}