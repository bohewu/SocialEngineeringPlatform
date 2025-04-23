using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SocialEngineeringPlatform.Web.Data;
using SocialEngineeringPlatform.Web.Models.Core;

// 加入授權

namespace SocialEngineeringPlatform.Web.Pages.MailTemplateCategories
{
    // [Authorize] // 如果需要，加上授權限制
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public MailTemplateCategory MailTemplateCategory { get; set; } = default!;

        // OnGet 用於載入要編輯的資料
        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // 使用 FindAsync 根據 Id 查找資料
            var mailtemplatecategory = await _context.MailTemplateCategories.FindAsync(id);
            if (mailtemplatecategory == null)
            {
                return NotFound();
            }
            MailTemplateCategory = mailtemplatecategory;
            return Page();
        }

        // OnPost 用於處理編輯表單的提交
        public async Task<IActionResult> OnPostAsync()
        {
            // 移除導覽屬性的驗證
            ModelState.Remove("MailTemplateCategory.MailTemplates");

            if (!ModelState.IsValid)
            {
                return Page(); // 驗證失敗，返回頁面顯示錯誤
            }

            // --- Fetch-Then-Update ---
            var categoryToUpdate = await _context.MailTemplateCategories.FindAsync(MailTemplateCategory.Id);

            if (categoryToUpdate == null)
            {
                return NotFound(); // 資料已被刪除
            }

            // 使用 TryUpdateModelAsync 更新允許修改的欄位
            if (await TryUpdateModelAsync<MailTemplateCategory>(
                categoryToUpdate,
                "MailTemplateCategory", // Prefix for form fields
                c => c.Name, c => c.Description)) // 指定要更新的欄位
            {
                // 注意：不需要手動設定 UpdateTime，因為模型中沒有這個欄位
                // 如果模型中有 UpdateTime，可以在這裡設定：
                // categoryToUpdate.UpdateTime = DateTime.UtcNow;

                try
                {
                    await _context.SaveChangesAsync(); // 儲存變更
                }
                catch (DbUpdateConcurrencyException)
                {
                    // 處理併發衝突
                    if (!MailTemplateCategoryExists(MailTemplateCategory.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                         ModelState.AddModelError(string.Empty, "此資料已被其他人修改，請重新載入後再試。");
                         return Page();
                        // throw;
                    }
                }
                return RedirectToPage("./Index"); // 成功後導向列表頁
            }

            return Page(); // TryUpdateModelAsync 失敗
        }

        private bool MailTemplateCategoryExists(int id)
        {
            return _context.MailTemplateCategories.Any(e => e.Id == id);
        }
    }
}