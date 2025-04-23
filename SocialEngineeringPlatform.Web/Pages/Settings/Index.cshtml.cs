using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SocialEngineeringPlatform.Web.Data; // For RoleAdmin
using SocialEngineeringPlatform.Web.Models.Core; // For DbSmtpSetting
using SocialEngineeringPlatform.Web.Services.Interfaces; // For ISettingsService

namespace SocialEngineeringPlatform.Web.Pages.Settings
{
    // 限制只有 Administrator 可以訪問
    [Authorize(Roles = ApplicationDbContext.RoleAdmin)]
    public class IndexModel : PageModel
    {
        private readonly ISettingsService _settingsService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ISettingsService settingsService, ILogger<IndexModel> logger)
        {
            _settingsService = settingsService;
            _logger = logger;
        }

        // 使用 InputModel 模式綁定表單資料
        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        // InputModel 包含 DbSmtpSetting 的欄位以及新密碼欄位
        public class InputModel
        {
            // Id 不需要顯示或編輯，但在更新時需要
            [HiddenInput]
            public int Id { get; set; } = 1; // 固定為 1

            [MaxLength(255)]
            [Display(Name = "SMTP 主機")]
            public string? Host { get; set; }

            [Required(ErrorMessage = "連接埠為必填。")]
            [Range(1, 65535, ErrorMessage = "連接埠必須介於 1 到 65535 之間。")]
            [Display(Name = "SMTP 連接埠")]
            public int Port { get; set; } = 587;

            [Display(Name = "啟用 SSL/TLS")]
            public bool EnableSsl { get; set; } = true;

            [MaxLength(255)]
            [Display(Name = "SMTP 使用者名稱 (Email)")]
            public string? Username { get; set; }

            // 新密碼欄位 (可選)
            [DataType(DataType.Password)]
            [Display(Name = "新 SMTP 密碼 (留空表示不變更)")]
            public string? NewPassword { get; set; }

            [Required(ErrorMessage = "寄件者 Email 為必填。")]
            [EmailAddress]
            [MaxLength(255)]
            [Display(Name = "預設寄件者 Email")]
            public string FromAddress { get; set; } = "";

            [MaxLength(255)]
            [Display(Name = "預設寄件者顯示名稱")]
            public string? FromDisplayName { get; set; }
        }

        // OnGet 用於載入目前的設定值
        public async Task<IActionResult> OnGetAsync()
        {
            var settings = await _settingsService.GetSmtpSettingsAsync();
            if (settings == null)
            {
                // 理論上 DbInitializer 會建立，但還是做個防禦性處理
                _logger.LogWarning("無法從資料庫載入 SMTP 設定，將使用預設值。");
                Input = new InputModel(); // 使用 InputModel 的預設值
            }
            else
            {
                // 將資料庫讀取的值填入 InputModel
                Input.Id = settings.Id; // 確保 ID 正確
                Input.Host = settings.Host;
                Input.Port = settings.Port;
                Input.EnableSsl = settings.EnableSsl;
                Input.Username = settings.Username;
                Input.FromAddress = settings.FromAddress;
                Input.FromDisplayName = settings.FromDisplayName;
                // 不載入 NewPassword
            }
            return Page();
        }

        // OnPost 用於處理設定儲存
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page(); // 返回頁面顯示驗證錯誤
            }

            // 將 InputModel 的值轉換回 DbSmtpSetting 物件
            var settingsToUpdate = new DbSmtpSetting
            {
                Id = Input.Id, // 確保傳遞正確的 ID
                Host = Input.Host,
                Port = Input.Port,
                EnableSsl = Input.EnableSsl,
                Username = Input.Username,
                FromAddress = Input.FromAddress,
                FromDisplayName = Input.FromDisplayName
                // EncryptedPassword 會在 UpdateSmtpSettingsAsync 中處理
            };

            // 呼叫服務更新設定，並傳入新密碼 (如果有的話)
            bool success = await _settingsService.UpdateSmtpSettingsAsync(settingsToUpdate, Input.NewPassword);

            if (success)
            {
                TempData["SuccessMessage"] = "SMTP 設定已成功儲存。";
                _logger.LogInformation("SMTP 設定已由管理員更新。");
            }
            else
            {
                TempData["ErrorMessage"] = "儲存 SMTP 設定時發生錯誤。";
                 // 可以考慮將服務層的錯誤細節傳遞回來顯示
                 // ModelState.AddModelError(string.Empty, "儲存時發生錯誤...");
            }

            // 重新導向回本頁面以顯示訊息
            return RedirectToPage();
        }
    }
}
