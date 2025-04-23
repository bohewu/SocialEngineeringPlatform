using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SocialEngineeringPlatform.Web.Data;
using SocialEngineeringPlatform.Web.Models.Core;

namespace SocialEngineeringPlatform.Web.Pages.LandingPageTemplates
{
    // [Authorize]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public LandingPageTemplate LandingPageTemplate { get; set; } = default!;

        // OnGet 用於載入要編輯的資料
        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // 查詢範本資料，並載入建立者資訊以顯示
            var landingpagetemplate = await _context.LandingPageTemplates
                                             .Include(l => l.CreatedByUser) // 載入建立者
                                             .FirstOrDefaultAsync(m => m.Id == id);
            if (landingpagetemplate == null)
            {
                return NotFound();
            }
            LandingPageTemplate = landingpagetemplate;
            return Page();
        }

        // OnPost 用於處理編輯表單的提交
        public async Task<IActionResult> OnPostAsync()
        {
            // 移除不需要驗證的導覽屬性
            ModelState.Remove("LandingPageTemplate.CreatedByUser");
            ModelState.Remove("LandingPageTemplate.Campaigns");
            ModelState.Remove("LandingPageTemplate.TrackingEvents");
            // 移除 CreatedByUserId 的驗證狀態 (因為是後端設定且不可修改)
            ModelState.Remove("LandingPageTemplate.CreatedByUserId");


            if (!ModelState.IsValid)
            {
                // 如果驗證失敗，返回頁面 (不需要重載下拉選單)
                return Page();
            }

            // --- Fetch-Then-Update ---
            var templateToUpdate = await _context.LandingPageTemplates.FindAsync(LandingPageTemplate.Id);

            if (templateToUpdate == null)
            {
                return NotFound(); // 資料已被刪除
            }

            // 使用 TryUpdateModelAsync 更新允許修改的欄位
            // 注意：不要包含 CreateTime 和 CreatedByUserId
            if (await TryUpdateModelAsync<LandingPageTemplate>(
                templateToUpdate,
                "LandingPageTemplate", // Prefix for form fields
                t => t.Name, t => t.HtmlContent, t => t.CollectFieldsConfig)) // 指定要更新的欄位
            {
                templateToUpdate.UpdateTime = DateTime.UtcNow; // 更新修改時間
                try
                {
                    await _context.SaveChangesAsync(); // 儲存變更
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LandingPageTemplateExists(LandingPageTemplate.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                         ModelState.AddModelError(string.Empty, "此資料已被其他人修改，請重新載入後再試。");
                         // 需要重新載入 CreatedByUser 以顯示唯讀欄位
                         // templateToUpdate = await _context.LandingPageTemplates.Include(m => m.CreatedByUser).FirstOrDefaultAsync(m => m.Id == LandingPageTemplate.Id);
                         // LandingPageTemplate = templateToUpdate; // 更新 BindProperty 的值以便重新顯示
                         return Page();
                        // throw;
                    }
                }
                return RedirectToPage("./Index"); // 成功後導向列表頁
            }

            // 如果 TryUpdateModelAsync 失敗
            return Page();
        }

        private bool LandingPageTemplateExists(int id)
        {
          return _context.LandingPageTemplates.Any(e => e.Id == id);
        }
    }
}