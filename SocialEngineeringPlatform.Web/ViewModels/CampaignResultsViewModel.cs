using SocialEngineeringPlatform.Web.Models.Core;
using System.ComponentModel.DataAnnotations;

namespace SocialEngineeringPlatform.Web.ViewModels
{
    // 用於傳遞單一活動報告頁面所需數據的 ViewModel
    public class CampaignResultsViewModel
    {
        public Campaign Campaign { get; set; } = default!; // 活動基本資訊

        [Display(Name = "目標總數")] public int TotalTargets { get; set; }

        [Display(Name = "成功發送數")] public int EmailsSent { get; set; }

        [Display(Name = "發送失敗數")] public int EmailsFailed { get; set; }

        [Display(Name = "開啟記錄數")] public int OpensRecorded { get; set; } // 唯一使用者開啟數

        [Display(Name = "點擊記錄數")] public int ClicksRecorded { get; set; } // 唯一使用者點擊數

        [Display(Name = "提交記錄數")] public int SubmissionsRecorded { get; set; } // 唯一使用者提交數

        [Display(Name = "回報記錄數")] public int ReportsRecorded { get; set; } // 唯一使用者回報數

        [Display(Name = "開啟率 (%)")]
        [DisplayFormat(DataFormatString = "{0:P1}")] // 格式化為百分比，小數點後 1 位
        public double OpenRate { get; set; }

        [Display(Name = "點擊率 (%)")]
        [DisplayFormat(DataFormatString = "{0:P1}")]
        public double ClickRate { get; set; } // 相對於成功發送數

        [Display(Name = "提交率 (%)")]
        [DisplayFormat(DataFormatString = "{0:P1}")]
        public double SubmissionRate { get; set; } // 相對於成功發送數

        [Display(Name = "目標互動詳情")]
        public List<TargetInteractionViewModel> TargetInteractions { get; set; } =
            new List<TargetInteractionViewModel>();
    }
}