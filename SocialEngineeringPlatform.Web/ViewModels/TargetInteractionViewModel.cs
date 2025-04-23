using SocialEngineeringPlatform.Web.Models.Enums;
using System.ComponentModel.DataAnnotations;
using CsvHelper.Configuration.Attributes;

namespace SocialEngineeringPlatform.Web.ViewModels
{
    // 用於顯示單一目標在活動中的互動情況
    public class TargetInteractionViewModel
    {
        [Ignore] // CsvHelper 忽略此欄位
        public int TargetUserId { get; set; }

        [Name("電子郵件")] // CSV 標頭名稱
        [Display(Name = "電子郵件")]
        public string Email { get; set; } = "";

        [Name("姓名")]
        [Display(Name = "姓名")]
        public string? Name { get; set; }

        [Name("發送狀態")]
        [Display(Name = "發送狀態")]
        public CampaignSendStatus SendStatus { get; set; } = CampaignSendStatus.Pending;

        [Name("發送時間")]
        [Display(Name = "發送時間")]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm:ss}")]
        [Format("yyyy-MM-dd HH:mm:ss")] // CsvHelper 日期格式
        public DateTime? SentTime { get; set; }

        [Name("已開啟")]
        [Display(Name = "已開啟")]
        [BooleanTrueValues("是")] // CsvHelper 布林值表示
        [BooleanFalseValues("否")]
        public bool Opened { get; set; } = false;

        [Name("已點擊")]
        [Display(Name = "已點擊")]
        [BooleanTrueValues("是")]
        [BooleanFalseValues("否")]
        public bool Clicked { get; set; } = false;

        [Name("已提交")]
        [Display(Name = "已提交")]
        [BooleanTrueValues("是")]
        [BooleanFalseValues("否")]
        public bool Submitted { get; set; } = false;

        [Name("已回報")]
        [Display(Name = "已回報")]
        [BooleanTrueValues("是")]
        [BooleanFalseValues("否")]
        public bool Reported { get; set; } = false;
    }
}