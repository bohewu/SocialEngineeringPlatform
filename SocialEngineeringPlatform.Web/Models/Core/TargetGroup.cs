using System.ComponentModel.DataAnnotations;

namespace SocialEngineeringPlatform.Web.Models.Core
{
    public class TargetGroup
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "群組名稱為必填欄位。")] // 可以加上中文錯誤訊息
        [MaxLength(100)]
        [Display(Name = "群組名稱")] // *** 指定顯示名稱 ***
        public required string Name { get; set; }

        [MaxLength(500)]
        [Display(Name = "描述")] // *** 指定顯示名稱 ***
        public string? Description { get; set; }

        [Display(Name = "建立時間")] // *** 指定顯示名稱 ***
        public DateTime CreateTime { get; set; } = DateTime.UtcNow;

        [Display(Name = "更新時間")] // *** 指定顯示名稱 ***
        public DateTime UpdateTime { get; set; } = DateTime.UtcNow;

        // 導覽屬性: 此群組包含的使用者
        public virtual ICollection<TargetUser> TargetUsers { get; set; } = new List<TargetUser>();
    }
}