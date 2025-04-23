using System.ComponentModel.DataAnnotations;

namespace SocialEngineeringPlatform.Web.Models.Core
{
    // 用於儲存 SMTP 設定到資料庫的模型
    // 假設系統只需要一組 SMTP 設定
    public class DbSmtpSetting
    {
        [Key]
        public int Id { get; set; } // 使用固定 ID (例如 1) 或確保只有一筆記錄

        [MaxLength(255)]
        [Display(Name = "SMTP 主機")]
        public string? Host { get; set; }

        [Display(Name = "SMTP 連接埠")]
        public int Port { get; set; } = 587; // 提供預設值

        [Display(Name = "啟用 SSL/TLS")]
        public bool EnableSsl { get; set; } = true; // 提供預設值

        [MaxLength(255)]
        [Display(Name = "SMTP 使用者名稱 (Email)")]
        public string? Username { get; set; }

        // 密碼會加密儲存，這裡不直接存放明文
        [Display(Name = "SMTP 密碼 (已加密)")]
        public string? EncryptedPassword { get; set; }

        [Required(ErrorMessage = "寄件者 Email 為必填。")]
        [EmailAddress]
        [MaxLength(255)]
        [Display(Name = "預設寄件者 Email")]
        public required string FromAddress { get; set; } = "noreply@example.com"; // 提供預設值

        [MaxLength(255)]
        [Display(Name = "預設寄件者顯示名稱")]
        public string? FromDisplayName { get; set; }
    }
}