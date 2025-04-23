using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SocialEngineeringPlatform.Web.Data;
using SocialEngineeringPlatform.Web.Models.Core;

// 加入授權

namespace SocialEngineeringPlatform.Web.Pages.MailTemplateCategories
{
    // [Authorize] // 如果需要，加上授權限制
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DeleteModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public MailTemplateCategory MailTemplateCategory { get; set; } = default!;

        // OnGet 用於顯示要刪除的資料供確認
        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mailtemplatecategory = await _context.MailTemplateCategories.FirstOrDefaultAsync(m => m.Id == id);

            if (mailtemplatecategory == null)
            {
                return NotFound();
            }
            else
            {
                MailTemplateCategory = mailtemplatecategory;
            }
            return Page();
        }

        // OnPost 用於處理刪除確認後的提交
        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // 再次查找確認資料存在
            var mailtemplatecategory = await _context.MailTemplateCategories.FindAsync(id);
            if (mailtemplatecategory != null)
            {
                // 注意：實際應用中可能需要檢查是否有 MailTemplate 正在使用此分類
                // 如果有關聯的 MailTemplate，根據需求可能阻止刪除或一併處理
                // 此處為簡化範例，直接刪除
                MailTemplateCategory = mailtemplatecategory; // 賦值給 BindProperty (可選)
                _context.MailTemplateCategories.Remove(MailTemplateCategory);
                await _context.SaveChangesAsync(); // 儲存變更

                // (可選) 設定成功訊息
                // TempData["SuccessMessage"] = "範本分類已成功刪除！";
            }

            return RedirectToPage("./Index"); // 返回列表頁
        }
    }
}