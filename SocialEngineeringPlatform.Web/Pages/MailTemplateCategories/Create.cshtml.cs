using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SocialEngineeringPlatform.Web.Data;
using SocialEngineeringPlatform.Web.Models.Core;

// 加入授權

namespace SocialEngineeringPlatform.Web.Pages.MailTemplateCategories
{
    // [Authorize] // 如果需要，加上授權限制
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // 返回建立頁面的空白表單
        public IActionResult OnGet()
        {
            MailTemplateCategory = new MailTemplateCategory
            {
                Name = string.Empty
            }; // 初始化
            return Page();
        }

        [BindProperty]
        public MailTemplateCategory MailTemplateCategory { get; set; } = default!;

        // 處理表單提交 (POST)
        public async Task<IActionResult> OnPostAsync()
        {
            // 移除導覽屬性的驗證，因為我們不會在表單中提交它
            ModelState.Remove("MailTemplateCategory.MailTemplates");

            if (!ModelState.IsValid)
            {
                return Page(); // 如果驗證失敗，返回頁面顯示錯誤
            }

            // 將新的分類加入 DbContext
            _context.MailTemplateCategories.Add(MailTemplateCategory);
            // 儲存變更到資料庫
            await _context.SaveChangesAsync();

            // (可選) 設定成功訊息
            // TempData["SuccessMessage"] = "範本分類已成功建立！";

            // 重新導向回列表頁
            return RedirectToPage("./Index");
        }
    }
}