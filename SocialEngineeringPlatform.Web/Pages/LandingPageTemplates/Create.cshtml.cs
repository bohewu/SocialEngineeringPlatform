using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SocialEngineeringPlatform.Web.Data;
using SocialEngineeringPlatform.Web.Models.Core;
using Microsoft.AspNetCore.Identity; // For UserManager

namespace SocialEngineeringPlatform.Web.Pages.LandingPageTemplates
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

        // OnGet 用於顯示空白表單
        public IActionResult OnGet()
        {
            LandingPageTemplate = new LandingPageTemplate
            {
                Name = string.Empty,
                HtmlContent = string.Empty,
                CreatedByUserId = string.Empty
            }; // 初始化
            return Page();
        }

        [BindProperty]
        public LandingPageTemplate LandingPageTemplate { get; set; } = default!;


        // OnPost 用於處理表單提交
        public async Task<IActionResult> OnPostAsync()
        {
            // 移除不需要驗證的導覽屬性
            ModelState.Remove("LandingPageTemplate.CreatedByUser");
            ModelState.Remove("LandingPageTemplate.Campaigns");
            ModelState.Remove("LandingPageTemplate.TrackingEvents");
            // 移除 CreatedByUserId 的驗證狀態 (因為是後端設定)
            ModelState.Remove("LandingPageTemplate.CreatedByUserId");


            // 自動設定建立者 ID
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                ModelState.AddModelError(string.Empty, "無法取得目前使用者資訊。");
                return Page();
            }
            LandingPageTemplate.CreatedByUserId = userId;

            // 設定時間戳記
            LandingPageTemplate.CreateTime = DateTime.UtcNow;
            LandingPageTemplate.UpdateTime = DateTime.UtcNow;

            // 重新驗證模型 (在設定 CreatedByUserId 之後)
            // TryValidateModel(LandingPageTemplate); // 可選，如果 [Required] 在 UserId 上

            if (!ModelState.IsValid)
            {
                return Page(); // 如果驗證失敗，返回頁面顯示錯誤
            }

            _context.LandingPageTemplates.Add(LandingPageTemplate);
            await _context.SaveChangesAsync();

            // TempData["SuccessMessage"] = "登陸頁範本已成功建立！";
            return RedirectToPage("./Index");
        }
    }
}