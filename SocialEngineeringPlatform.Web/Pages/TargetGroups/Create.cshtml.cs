using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SocialEngineeringPlatform.Web.Data;
using SocialEngineeringPlatform.Web.Models.Core;

// 加入授權

namespace SocialEngineeringPlatform.Web.Pages.TargetGroups
{
    // [Authorize] // 如果尚未在 Program.cs 中設定 AuthorizeFolder("/TargetGroups")，則需要加上此屬性
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // OnGet 方法只負責顯示空白的建立表單頁面
        public IActionResult OnGet()
        {
            // 初始化 TargetGroup，可以設定預設值 (如果需要)
            TargetGroup = new TargetGroup
            {
                Name = string.Empty
            };
            return Page();
        }

        // 使用 [BindProperty] 將表單送過來的資料綁定到這個屬性上
        [BindProperty]
        public TargetGroup TargetGroup { get; set; } = default!;

        // 當表單以 POST 方式提交時執行
        public async Task<IActionResult> OnPostAsync()
        {
            // 清除不需要模型繫結的導覽屬性狀態，避免驗證錯誤 (如果有的話)
            ModelState.Remove("TargetGroup.TargetUsers");

            // 檢查模型驗證狀態 (例如 [Required] 等是否有通過)
            if (!ModelState.IsValid)
            {
                // 如果驗證失敗，停留在目前頁面，顯示錯誤訊息
                return Page();
            }

            // 設定建立時間與更新時間 (雖然資料庫有預設值，但在應用層設定更明確)
            TargetGroup.CreateTime = DateTime.UtcNow;
            TargetGroup.UpdateTime = DateTime.UtcNow;

            // 將新的 TargetGroup 物件加入 DbContext
            _context.TargetGroups.Add(TargetGroup);
            // 非同步儲存變更到資料庫
            await _context.SaveChangesAsync();

            // (可選) 設定成功訊息，例如使用 TempData
            // TempData["SuccessMessage"] = "目標群組已成功建立！";

            // 操作成功後，重新導向到 Index 列表頁面
            return RedirectToPage("./Index");
        }
    }
}