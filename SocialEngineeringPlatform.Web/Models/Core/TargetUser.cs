using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SocialEngineeringPlatform.Web.Models.Core
{
    public class TargetUser
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Email 為必填欄位。")]
        [EmailAddress]
        [MaxLength(256)]
        [Display(Name = "電子郵件")] // 中文標籤
        public required string Email { get; set; }

        [MaxLength(256)]
        [Display(Name = "姓名")] // 中文標籤
        public string? Name { get; set; }

        [Display(Name = "所屬群組ID")] // 中文標籤 (ID)
        public int? GroupId { get; set; } // 外鍵 (可為空)

        [MaxLength(256)]
        [Display(Name = "自訂欄位1")] // 中文標籤
        public string? CustomField1 { get; set; }

        [MaxLength(256)]
        [Display(Name = "自訂欄位2")] // 中文標籤
        public string? CustomField2 { get; set; }

        [Display(Name = "啟用狀態")] // 中文標籤
        public bool IsActive { get; set; } = true;

        [Display(Name = "建立時間")] // 中文標籤
        public DateTime CreateTime { get; set; } = DateTime.UtcNow;

        [Display(Name = "更新時間")] // 中文標籤
        public DateTime UpdateTime { get; set; } = DateTime.UtcNow;

        // 導覽屬性 (Navigation Property)
        [ForeignKey("GroupId")]
        [Display(Name = "所屬群組")] // 中文標籤 (關聯物件)
        public virtual TargetGroup? Group { get; set; } // 對應到 TargetGroup

        // 其他導覽屬性
        public virtual ICollection<CampaignTarget> CampaignTargets { get; set; } = new List<CampaignTarget>();
        public virtual ICollection<TrackingEvent> TrackingEvents { get; set; } = new List<TrackingEvent>();
        public virtual ICollection<MailSendLog> MailSendLogs { get; set; } = new List<MailSendLog>();
    }
}