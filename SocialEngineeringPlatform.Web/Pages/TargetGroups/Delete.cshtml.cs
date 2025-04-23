using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SocialEngineeringPlatform.Web.Data;
using SocialEngineeringPlatform.Web.Models.Core;

// 加入授權

namespace SocialEngineeringPlatform.Web.Pages.TargetGroups
{
    // [Authorize] // 如果尚未在 Program.cs 中設定 AuthorizeFolder("/TargetGroups")，則需要加上此屬性
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DeleteModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // 使用 [BindProperty] 讓頁面可以存取 TargetGroup 資料，並在 POST 時傳回 Id
        [BindProperty]
        public TargetGroup TargetGroup { get; set; } = default!;

        // 當頁面以 GET 方式請求時執行，用於顯示要刪除的資料
        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // 查詢要刪除的目標群組
            var targetgroup = await _context.TargetGroups.FirstOrDefaultAsync(m => m.Id == id);

            if (targetgroup == null)
            {
                return NotFound();
            }
            else
            {
                TargetGroup = targetgroup;
            }
            return Page();
        }

        // 當使用者按下「刪除」按鈕，表單以 POST 方式提交時執行
        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // 再次查詢確認資料庫中確實存在這筆資料
            // 使用 FindAsync 是因為我們只需要根據 Id 操作
            var targetGroupToDelete = await _context.TargetGroups.FindAsync(id);

            if (targetGroupToDelete != null)
            {
                // 如果資料存在，則將其從 DbContext 中移除
                TargetGroup = targetGroupToDelete; // 將找到的實體賦值給 BindProperty (可選)
                _context.TargetGroups.Remove(TargetGroup);
                // 儲存變更到資料庫
                await _context.SaveChangesAsync();

                 // (可選) 設定成功訊息
                // TempData["SuccessMessage"] = "目標群組已成功刪除！";
            }
            // else: 如果 targetGroupToDelete 是 null，表示在 GET 和 POST 之間資料已被刪除，
            //       直接導向 Index 即可，無需做任何事或報錯。

            // 操作完成後，重新導向到 Index 列表頁面
            return RedirectToPage("./Index");
        }
    }
}