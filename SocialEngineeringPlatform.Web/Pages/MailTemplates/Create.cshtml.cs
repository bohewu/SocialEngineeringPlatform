// For List
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering; // For SelectList, SelectListItem
using Microsoft.EntityFrameworkCore;
using SocialEngineeringPlatform.Web.Data;
using SocialEngineeringPlatform.Web.Models.Core;
using Microsoft.AspNetCore.Identity;

namespace SocialEngineeringPlatform.Web.Pages.MailTemplates
{
    // [Authorize]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CreateModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // OnGet 用於顯示空白表單，並準備下拉選單
        public async Task<IActionResult> OnGetAsync()
        {
            await PopulateCategoriesDropDownListAsync(_context); // 準備分類下拉選單
            PopulateLanguageDropDownList(); // *** 準備語言下拉選單 ***
            MailTemplate = new MailTemplate
            {
                Name = string.Empty,
                Subject = string.Empty,
                Body = string.Empty,
                CreatedByUserId = string.Empty
            }; // 初始化模型
            return Page();
        }

        [BindProperty]
        public MailTemplate MailTemplate { get; set; } = default!;

        // 分類下拉選單的選項
        public SelectList? CategoryNameSL { get; set; }
        // *** 新增：語言下拉選單的選項 ***
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

        // *** 新增：輔助方法：準備語言下拉選單資料 ***
        private void PopulateLanguageDropDownList(object? selectedLanguage = null)
        {
            // 定義預設的語言選項
            var languages = new List<SelectListItem>
            {
                new SelectListItem { Value = "zh-TW", Text = "繁體中文 (台灣) zh-TW" },
                new SelectListItem { Value = "zh-CN", Text = "简体中文 (中国) zh-CN" },
                new SelectListItem { Value = "en-US", Text = "English (United States) en-US" },
                new SelectListItem { Value = "ja-JP", Text = "日本語 (日本) ja-JP" },
                new SelectListItem { Value = "ko-KR", Text = "한국어 (대한민국) ko-KR" }
                // 可以根據需要增加更多語言選項
            };

            // 建立 SelectList，Value 對應 MailTemplate.Language 屬性，Text 是顯示文字
            LanguageSL = new SelectList(languages, "Value", "Text", selectedLanguage);
        }


        // OnPost 用於處理表單提交
        public async Task<IActionResult> OnPostAsync()
        {
            // 移除不需要驗證的導覽屬性
            ModelState.Remove("MailTemplate.Category");
            ModelState.Remove("MailTemplate.CreatedByUser");
            ModelState.Remove("MailTemplate.Campaigns");
            ModelState.Remove("MailTemplate.TrackingEvents");
            ModelState.Remove("MailTemplate.CreatedByUserId");

            // 自動設定建立者 ID
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                ModelState.AddModelError(string.Empty, "無法取得目前使用者資訊。");
                await PopulateCategoriesDropDownListAsync(_context, MailTemplate.CategoryId);
                PopulateLanguageDropDownList(MailTemplate.Language); // *** 重新載入語言下拉選單 ***
                return Page();
            }
            MailTemplate.CreatedByUserId = userId;

            // 設定時間戳記
            MailTemplate.CreateTime = DateTime.UtcNow;
            MailTemplate.UpdateTime = DateTime.UtcNow;


            if (!ModelState.IsValid)
            {
                // 如果驗證失敗，重新載入下拉選單資料並返回頁面
                await PopulateCategoriesDropDownListAsync(_context, MailTemplate.CategoryId);
                PopulateLanguageDropDownList(MailTemplate.Language); // *** 重新載入語言下拉選單 ***
                return Page();
            }

            _context.MailTemplates.Add(MailTemplate);
            await _context.SaveChangesAsync();

            // TempData["SuccessMessage"] = "郵件範本已成功建立！";
            return RedirectToPage("./Index");
        }
    }
}