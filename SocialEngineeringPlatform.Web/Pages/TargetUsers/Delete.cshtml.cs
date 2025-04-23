using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore; // 需要引用 EntityFrameworkCore
using SocialEngineeringPlatform.Web.Data;
using SocialEngineeringPlatform.Web.Models.Core;

// 加入授權

namespace SocialEngineeringPlatform.Web.Pages.TargetUsers
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
        public TargetUser TargetUser { get; set; } = default!;

        // OnGet 用於顯示要刪除的資料供確認
        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // 查詢要刪除的使用者資料，並載入關聯的 Group 以顯示名稱
            var targetuser = await _context.TargetUsers
                .Include(t => t.Group) // 載入關聯群組
                .FirstOrDefaultAsync(m => m.Id == id);

            if (targetuser == null)
            {
                return NotFound();
            }
            else
            {
                TargetUser = targetuser;
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

            // 查找要刪除的使用者
            var targetUserToDelete = await _context.TargetUsers.FindAsync(id);

            if (targetUserToDelete != null)
            {
                // 注意：實際應用中可能需要檢查是否有 CampaignTarget 或 TrackingEvent 關聯到此使用者
                // 根據需求可能阻止刪除或進行相應處理 (例如設為 Inactive 而非真刪除)
                // 此處為簡化範例，直接刪除
                TargetUser = targetUserToDelete; // 賦值給 BindProperty (可選)
                _context.TargetUsers.Remove(TargetUser);
                await _context.SaveChangesAsync(); // 儲存變更

                // (可選) 設定成功訊息
                // TempData["SuccessMessage"] = "目標使用者已成功刪除！";
            }
            // else: 如果找不到，表示已被刪除，直接返回列表即可

            return RedirectToPage("./Index"); // 返回列表頁
        }
    }
}