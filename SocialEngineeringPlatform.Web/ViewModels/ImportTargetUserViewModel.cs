using System.ComponentModel.DataAnnotations;

namespace SocialEngineeringPlatform.Web.ViewModels
{
    // 用於映射 CSV 檔案中的一行資料
    public class ImportTargetUserViewModel
    {
        // CsvHelper 會根據 Header 名稱自動映射 (大小寫不敏感，會忽略空格和底線)
        // 或者可以使用 CsvHelper.Configuration.Attributes.NameAttribute 來指定對應的欄位名稱

        [Required(ErrorMessage = "Email 為必填。")]
        [EmailAddress(ErrorMessage = "Email 格式不正確。")]
        [Display(Name = "電子郵件")]
        public string? Email { get; set; } // 設為 nullable 以便檢查是否為空

        [Display(Name = "姓名")]
        public string? Name { get; set; }

        [Display(Name = "所屬群組名稱")] // CSV 中可能提供群組名稱而非 ID
        public string? GroupName { get; set; }

        [Display(Name = "自訂欄位1")] // 中文標籤
        public string? CustomField1 { get; set; }

        [Display(Name = "自訂欄位2")] // 中文標籤
        public string? CustomField2 { get; set; }

        // --- 以下屬性用於處理過程 ---
        [System.ComponentModel.DataAnnotations.Schema.NotMapped] // 不映射到資料庫
        public int RowNumber { get; set; } // 記錄在 CSV 中的行號

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public bool IsValid { get; set; } = true; // 預設有效

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public List<string> Errors { get; set; } = new List<string>(); // 記錄錯誤訊息
    }
}