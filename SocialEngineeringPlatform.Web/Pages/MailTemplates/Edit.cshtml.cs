using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering; // For SelectList, SelectListItem
using Microsoft.EntityFrameworkCore;
using SocialEngineeringPlatform.Web.Data;
using SocialEngineeringPlatform.Web.Models.Core;
// For UserManager (Optional here, could display creator info)

namespace SocialEngineeringPlatform.Web.Pages.MailTemplates
{
    // [Authorize]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        // private readonly UserManager<ApplicationUser> _userManager; // 如果需要顯示建立者資訊可以注入

        public EditModel(ApplicationDbContext context) // UserManager<ApplicationUser> userManager)
        {
            _context = context;
            // _userManager = userManager;
        }

        [BindProperty]
        public MailTemplate MailTemplate { get; set; } = default!;

        // 下拉選單選項
        public SelectList? CategoryNameSL { get; set; }
        public SelectList? LanguageSL { get; set; }

        // 輔助方法：準備分類下拉選單資料
        private async Task PopulateCategoriesDropDownListAsync(ApplicationDbContext context, object? selectedCategory = null)
        {
            var categoriesQuery = from c in context.MailTemplateCategories
                                  orderby c.Name
                                  select new { c.Id, c.Name };

            CategoryNameSL = new SelectList(await categoriesQuery.AsNoTracking().ToListAsync(),
                                            "Id", "Name", selectedCategory);
        }

        // 輔助方法：準備語言下拉選單資料
        private void PopulateLanguageDropDownList(object? selectedLanguage = null)
        {
            var languages = new List<SelectListItem>
            {
                new SelectListItem { Value = "zh-TW", Text = "繁體中文 (台灣) zh-TW" },
                new SelectListItem { Value = "zh-CN", Text = "简体中文 (中国) zh-CN" },
                new SelectListItem { Value = "en-US", Text = "English (United States) en-US" },
                new SelectListItem { Value = "ja-JP", Text = "日本語 (日本) ja-JP" },
                new SelectListItem { Value = "ko-KR", Text = "한국어 (대한민국) ko-KR" }
            };
            LanguageSL = new SelectList(languages, "Value", "Text", selectedLanguage);
        }

        // OnGet 用於載入要編輯的資料和下拉選單
        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // 查詢範本資料，同時載入建立者資訊以便顯示 (可選)
            var mailtemplate = await _context.MailTemplates
                                     .Include(m => m.CreatedByUser) // 載入建立者以顯示 Email
                                     .FirstOrDefaultAsync(m => m.Id == id);

            if (mailtemplate == null)
            {
                return NotFound();
            }
            MailTemplate = mailtemplate;

            // 準備下拉選單，並設定預選值
            await PopulateCategoriesDropDownListAsync(_context, MailTemplate.CategoryId);
            PopulateLanguageDropDownList(MailTemplate.Language);
            return Page();
        }

        // OnPost 用於處理編輯表單的提交
        public async Task<IActionResult> OnPostAsync()
        {
            // 移除不需要驗證的導覽屬性
            ModelState.Remove("MailTemplate.Category");
            ModelState.Remove("MailTemplate.CreatedByUser");
            ModelState.Remove("MailTemplate.Campaigns");
            ModelState.Remove("MailTemplate.TrackingEvents");
            // *** 新增：移除 CreatedByUserId 的驗證狀態 ***
            ModelState.Remove("MailTemplate.CreatedByUserId");


            // 驗證除了移除的屬性外的其他欄位
            if (!ModelState.IsValid)
            {
                // *** 如果驗證失敗，需要重新載入下拉選單資料 ***
                await PopulateCategoriesDropDownListAsync(_context, MailTemplate.CategoryId);
                PopulateLanguageDropDownList(MailTemplate.Language);
                return Page();
            }

            // --- Fetch-Then-Update ---
            // 注意：這裡不需要 Include，因為我們只更新，不顯示關聯資料
            var templateToUpdate = await _context.MailTemplates.FindAsync(MailTemplate.Id);

            if (templateToUpdate == null)
            {
                return NotFound(); // 資料已被刪除
            }

            // 使用 TryUpdateModelAsync 更新允許修改的欄位
            // 注意：不要包含 CreateTime 和 CreatedByUserId
            if (await TryUpdateModelAsync<MailTemplate>(
                templateToUpdate,
                "MailTemplate", // Prefix for form fields
                t => t.Name, t => t.Subject, t => t.Body, t => t.CategoryId, t => t.Language, t => t.AttachmentPath,
                t => t.CustomFromAddress, t => t.CustomFromDisplayName))
            {
                templateToUpdate.UpdateTime = DateTime.UtcNow; // 更新修改時間
                try
                {
                    await _context.SaveChangesAsync(); // 儲存變更
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MailTemplateExists(MailTemplate.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                         ModelState.AddModelError(string.Empty, "此資料已被其他人修改，請重新載入後再試。");
                         // 重新載入下拉選單
                         await PopulateCategoriesDropDownListAsync(_context, MailTemplate.CategoryId);
                         PopulateLanguageDropDownList(MailTemplate.Language);
                         // 需要重新載入 CreatedByUser 以顯示唯讀欄位 (如果 OnGetAsync 有載入的話)
                         // templateToUpdate = await _context.MailTemplates.Include(m => m.CreatedByUser).FirstOrDefaultAsync(m => m.Id == MailTemplate.Id);
                         // MailTemplate = templateToUpdate; // 更新 BindProperty 的值以便重新顯示
                         return Page();
                        // throw;
                    }
                }
                return RedirectToPage("./Index"); // 成功後導向列表頁
            }

            // 如果 TryUpdateModelAsync 失敗，重新載入下拉選單並返回頁面
            await PopulateCategoriesDropDownListAsync(_context, MailTemplate.CategoryId);
            PopulateLanguageDropDownList(MailTemplate.Language);
            return Page();
        }

        private bool MailTemplateExists(int id)
        {
          return _context.MailTemplates.Any(e => e.Id == id);
        }
    }
}