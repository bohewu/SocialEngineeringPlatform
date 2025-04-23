using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SocialEngineeringPlatform.Web.Data;
using SocialEngineeringPlatform.Web.Models.Core;

namespace SocialEngineeringPlatform.Web.Pages.LandingPageTemplates
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
        public LandingPageTemplate LandingPageTemplate { get; set; } = default!;

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

            // 查詢範本資料，並載入建立者資訊以顯示
            var landingpagetemplate = await _context.LandingPageTemplates
                .Include(l => l.CreatedByUser)
                .AsNoTracking() // 只是顯示，不需要追蹤
                .FirstOrDefaultAsync(m => m.Id == id);

            if (landingpagetemplate == null)
            {
                return NotFound();
            }

            LandingPageTemplate = landingpagetemplate;

            // *** 檢查是否有活動正在使用此範本 ***
            bool hasAssociatedCampaigns = await _context.Campaigns.AnyAsync(c => c.LandingPageTemplateId == id);
            if (hasAssociatedCampaigns)
            {
                CanDelete = false; // 如果有關聯活動，則不能刪除
                DeletionWarningMessage = "無法刪除：此登陸頁範本正被一個或多個演練活動使用中。請先解除關聯。";
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
            bool hasAssociatedCampaigns = await _context.Campaigns.AnyAsync(c => c.LandingPageTemplateId == id);
            if (hasAssociatedCampaigns)
            {
                // 如果有關聯活動，設定錯誤訊息並返回頁面
                ModelState.AddModelError(string.Empty, "無法刪除：此登陸頁範本正被一個或多個演練活動使用中。");
                // 需要重新載入資料以顯示頁面
                var landingpagetemplate = await _context.LandingPageTemplates
                                         .Include(l => l.CreatedByUser)
                                         .AsNoTracking()
                                         .FirstOrDefaultAsync(m => m.Id == id);
                if (landingpagetemplate != null) LandingPageTemplate = landingpagetemplate;
                CanDelete = false; // 確保按鈕狀態正確
                DeletionWarningMessage = "無法刪除：此登陸頁範本正被一個或多個演練活動使用中。請先解除關聯。";
                return Page();
            }


            // 查找要刪除的範本
            var landingPageTemplateToDelete = await _context.LandingPageTemplates.FindAsync(id);

            if (landingPageTemplateToDelete != null)
            {
                 // 確認沒有關聯後，執行刪除
                _context.LandingPageTemplates.Remove(landingPageTemplateToDelete);
                await _context.SaveChangesAsync();
                // TempData["SuccessMessage"] = "登陸頁範本已成功刪除！";
            }
            // else: 找不到表示已被刪除

            return RedirectToPage("./Index");
        }
    }
}