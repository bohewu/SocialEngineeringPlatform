using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SocialEngineeringPlatform.Web.Data;
using SocialEngineeringPlatform.Web.Models.Core;

namespace SocialEngineeringPlatform.Web.Pages.MailTemplates
{
    // [Authorize]
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DeleteModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public MailTemplate MailTemplate { get; set; } = default!;

        // 用於在 View 中判斷是否能刪除
        public bool CanDelete { get; set; } = true;
        public string? DeletionWarningMessage { get; set; }

        // OnGet 用於顯示要刪除的資料供確認
        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // 查詢範本資料，並載入關聯資訊以顯示
            var mailtemplate = await _context.MailTemplates
                .Include(m => m.Category)
                .Include(m => m.CreatedByUser)
                .AsNoTracking() // 因為只是顯示，不需要追蹤變更
                .FirstOrDefaultAsync(m => m.Id == id);

            if (mailtemplate == null)
            {
                return NotFound();
            }

            MailTemplate = mailtemplate;

            // *** 檢查是否有活動正在使用此範本 ***
            bool hasAssociatedCampaigns = await _context.Campaigns.AnyAsync(c => c.MailTemplateId == id);
            if (hasAssociatedCampaigns)
            {
                CanDelete = false; // 如果有關聯活動，則不能刪除
                DeletionWarningMessage = "無法刪除：此郵件範本正被一個或多個演練活動使用中。請先解除關聯。";
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

            // *** 再次檢查是否有活動正在使用此範本 ***
            bool hasAssociatedCampaigns = await _context.Campaigns.AnyAsync(c => c.MailTemplateId == id);
            if (hasAssociatedCampaigns)
            {
                // 如果有關聯活動，設定錯誤訊息並返回頁面
                ModelState.AddModelError(string.Empty, "無法刪除：此郵件範本正被一個或多個演練活動使用中。");
                // 需要重新載入資料以顯示頁面
                var mailtemplate = await _context.MailTemplates
                                         .Include(m => m.Category)
                                         .Include(m => m.CreatedByUser)
                                         .AsNoTracking()
                                         .FirstOrDefaultAsync(m => m.Id == id);
                if (mailtemplate != null) MailTemplate = mailtemplate;
                CanDelete = false; // 確保按鈕狀態正確
                DeletionWarningMessage = "無法刪除：此郵件範本正被一個或多個演練活動使用中。請先解除關聯。";
                return Page();
            }

            // 查找要刪除的範本
            var mailTemplateToDelete = await _context.MailTemplates.FindAsync(id);

            if (mailTemplateToDelete != null)
            {
                // 確認沒有關聯後，執行刪除
                _context.MailTemplates.Remove(mailTemplateToDelete);
                await _context.SaveChangesAsync();
                // TempData["SuccessMessage"] = "郵件範本已成功刪除！";
            }
            // else: 找不到表示已被刪除

            return RedirectToPage("./Index");
        }
    }
}