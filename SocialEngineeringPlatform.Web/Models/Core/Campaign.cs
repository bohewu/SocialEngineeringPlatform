using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SocialEngineeringPlatform.Web.Models.Enums;

namespace SocialEngineeringPlatform.Web.Models.Core
{
    public class Campaign
    {
        // ... (保留 Id, Name, Description 等其他屬性) ...
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "活動名稱為必填欄位。")]
        [MaxLength(256)]
        [Display(Name = "活動名稱")]
        public required string Name { get; set; }

        [MaxLength(1000)]
        [Display(Name = "描述")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "必須選擇一個郵件範本。")]
        [Display(Name = "郵件範本ID")]
        public int MailTemplateId { get; set; }

        [Display(Name = "登陸頁範本ID")]
        public int? LandingPageTemplateId { get; set; }

        // *** 新增：目標群組 ID (可選) ***
        [Display(Name = "目標群組ID")]
        public int? TargetGroupId { get; set; } // Nullable int

        [Display(Name = "預定發送時間")]
        public DateTime? ScheduledSendTime { get; set; }

        [Display(Name = "實際開始時間")]
        public DateTime? ActualStartTime { get; set; }

        [Display(Name = "結束時間")]
        public DateTime? EndTime { get; set; }

        [Required]
        [Display(Name = "狀態")]
        public required CampaignStatus Status { get; set; }

        [Display(Name = "自動發送")]
        public bool IsAutoSend { get; set; }

        [Display(Name = "批次間隔(秒)")]
        public int SendBatchDelaySeconds { get; set; } = 0;

        [Display(Name = "追蹤開啟事件")]
        public bool TrackOpens { get; set; } = true;
        
        // *** 新增：用於儲存背景工作 ID 的欄位 ***
        [MaxLength(100)] // Hangfire JobId 通常不會太長
        [Display(Name = "背景工作 ID")]
        public string? JobId { get; set; } // 使用通用的 JobId 名稱
        
        [Display(Name = "建立時間")]
        public DateTime CreateTime { get; set; } = DateTime.UtcNow;

        [Display(Name = "更新時間")]
        public DateTime UpdateTime { get; set; } = DateTime.UtcNow;

        [Required]
        [Display(Name = "建立者ID")]
        public required string CreatedByUserId { get; set; }

        // --- 導覽屬性 ---
        [ForeignKey("MailTemplateId")]
        [Display(Name = "郵件範本")]
        public virtual MailTemplate? MailTemplate { get; set; }

        [ForeignKey("LandingPageTemplateId")]
        [Display(Name = "登陸頁範本")]
        public virtual LandingPageTemplate? LandingPageTemplate { get; set; }

        // *** 新增：目標群組的導覽屬性 ***
        [ForeignKey("TargetGroupId")]
        [Display(Name = "目標群組")]
        public virtual TargetGroup? TargetGroup { get; set; }

        [ForeignKey("CreatedByUserId")]
        [Display(Name = "建立者")]
        public virtual ApplicationUser? CreatedByUser { get; set; }

        public virtual ICollection<CampaignTarget> CampaignTargets { get; set; } = new List<CampaignTarget>();
        public virtual ICollection<TrackingEvent> TrackingEvents { get; set; } = new List<TrackingEvent>();
        public virtual ICollection<MailSendLog> MailSendLogs { get; set; } = new List<MailSendLog>();
    }
}