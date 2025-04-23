using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering; // 需要引用 Rendering 才能使用 SelectList
using Microsoft.EntityFrameworkCore; // 需要引用 EntityFrameworkCore
using SocialEngineeringPlatform.Web.Data;
using SocialEngineeringPlatform.Web.Models.Core;

// 加入授權

namespace SocialEngineeringPlatform.Web.Pages.TargetUsers
{
    // [Authorize] // 如果需要，加上授權限制
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // OnGet 用於顯示空白表單，並準備下拉選單資料
        public async Task<IActionResult> OnGetAsync()
        {
            // 準備目標群組下拉選單的資料
            await PopulateGroupsDropDownListAsync(_context);
            TargetUser = new TargetUser
            {
                Email = string.Empty
            }; // 初始化模型
            return Page();
        }

        [BindProperty]
        public TargetUser TargetUser { get; set; } = default!;

        // 用於存放下拉選單選項的屬性
        public SelectList? GroupNameSL { get; set; }

        // 輔助方法：準備下拉選單資料
        private async Task PopulateGroupsDropDownListAsync(ApplicationDbContext context, object? selectedGroup = null)
        {
            // 從資料庫查詢所有 TargetGroups，只選取 Id 和 Name
            var groupsQuery = from g in context.TargetGroups
                              orderby g.Name // 依照名稱排序
                              select new { g.Id, g.Name };

            // 建立 SelectList 物件
            // 第一個參數是資料來源
            // 第二個參數是選項的 value 對應到的屬性名稱 (也就是 TargetUser.GroupId 要存的值)
            // 第三個參數是選項顯示的文字對應到的屬性名稱
            // 第四個參數是預設選取的項目 (用於編輯頁面，建立頁面通常是 null)
            GroupNameSL = new SelectList(await groupsQuery.AsNoTracking().ToListAsync(),
                                         "Id", "Name", selectedGroup);
        }


        // OnPost 用於處理表單提交
        public async Task<IActionResult> OnPostAsync()
        {
            // 移除不需要驗證的導覽屬性
            ModelState.Remove("TargetUser.Group");
            ModelState.Remove("TargetUser.CampaignTargets");
            ModelState.Remove("TargetUser.TrackingEvents");
            ModelState.Remove("TargetUser.MailSendLogs");


            if (!ModelState.IsValid)
            {
                // *** 重要：如果驗證失敗，需要重新載入下拉選單資料 ***
                await PopulateGroupsDropDownListAsync(_context, TargetUser.GroupId);
                return Page(); // 返回頁面顯示錯誤
            }

            // 設定時間戳記
            TargetUser.CreateTime = DateTime.UtcNow;
            TargetUser.UpdateTime = DateTime.UtcNow;

            _context.TargetUsers.Add(TargetUser);
            await _context.SaveChangesAsync();

            // TempData["SuccessMessage"] = "目標使用者已成功建立！";
            return RedirectToPage("./Index");
        }
    }
}