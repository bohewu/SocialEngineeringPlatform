// --- Pages/TargetUsers/Edit.cshtml.cs ---
// (請將此檔案放置在 Pages/TargetUsers 資料夾下)

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering; // For SelectList
using Microsoft.EntityFrameworkCore;
using SocialEngineeringPlatform.Web.Data;
using SocialEngineeringPlatform.Web.Models.Core;

// 加入授權

namespace SocialEngineeringPlatform.Web.Pages.TargetUsers
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
        public TargetUser TargetUser { get; set; } = default!;

        // 用於存放下拉選單選項的屬性
        public SelectList? GroupNameSL { get; set; }

        // 輔助方法：準備下拉選單資料
        private async Task PopulateGroupsDropDownListAsync(ApplicationDbContext context, object? selectedGroup = null)
        {
            var groupsQuery = from g in context.TargetGroups
                              orderby g.Name
                              select new { g.Id, g.Name };

            GroupNameSL = new SelectList(await groupsQuery.AsNoTracking().ToListAsync(),
                                         "Id", "Name", selectedGroup);
        }

        // OnGet 用於載入要編輯的資料和下拉選單
        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // 查詢使用者資料，不需要 Include Group，因為我們只需要 GroupId
            var targetuser = await _context.TargetUsers.FindAsync(id);
            if (targetuser == null)
            {
                return NotFound();
            }
            TargetUser = targetuser;

            // 準備下拉選單，並將目前使用者的 GroupId 設為預選項目
            await PopulateGroupsDropDownListAsync(_context, TargetUser.GroupId);
            return Page();
        }

        // OnPost 用於處理編輯表單的提交
        public async Task<IActionResult> OnPostAsync()
        {
            // 移除不需要驗證的導覽屬性
            ModelState.Remove("TargetUser.Group");
            ModelState.Remove("TargetUser.CampaignTargets");
            ModelState.Remove("TargetUser.TrackingEvents");
            ModelState.Remove("TargetUser.MailSendLogs");

            if (!ModelState.IsValid)
            {
                // *** 如果驗證失敗，需要重新載入下拉選單資料 ***
                await PopulateGroupsDropDownListAsync(_context, TargetUser.GroupId);
                return Page();
            }

            // --- Fetch-Then-Update ---
            var userToUpdate = await _context.TargetUsers.FindAsync(TargetUser.Id);

            if (userToUpdate == null)
            {
                return NotFound(); // 資料已被刪除
            }

            // 使用 TryUpdateModelAsync 更新允許修改的欄位
            if (await TryUpdateModelAsync<TargetUser>(
                userToUpdate,
                "TargetUser", // Prefix for form fields
                // 指定要從表單更新的欄位
                u => u.Email, u => u.Name, u => u.GroupId, u => u.CustomField1, u => u.CustomField2, u => u.IsActive))
            {
                userToUpdate.UpdateTime = DateTime.UtcNow; // 更新修改時間
                try
                {
                    await _context.SaveChangesAsync(); // 儲存變更
                }
                catch (DbUpdateConcurrencyException)
                {
                    // 處理併發衝突
                    if (!TargetUserExists(TargetUser.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                         ModelState.AddModelError(string.Empty, "此資料已被其他人修改，請重新載入後再試。");
                         // 重新載入下拉選單
                         await PopulateGroupsDropDownListAsync(_context, TargetUser.GroupId);
                         return Page();
                        // throw;
                    }
                }
                return RedirectToPage("./Index"); // 成功後導向列表頁
            }

            // 如果 TryUpdateModelAsync 失敗，重新載入下拉選單並返回頁面
            await PopulateGroupsDropDownListAsync(_context, TargetUser.GroupId);
            return Page();
        }

        private bool TargetUserExists(int id)
        {
            return _context.TargetUsers.Any(e => e.Id == id);
        }
    }
}