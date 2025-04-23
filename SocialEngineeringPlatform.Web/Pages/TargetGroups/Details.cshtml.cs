using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SocialEngineeringPlatform.Web.Data;
using SocialEngineeringPlatform.Web.Models.Core;

// 加入授權

namespace SocialEngineeringPlatform.Web.Pages.TargetGroups
{
    // [Authorize] // 如果尚未在 Program.cs 中設定 AuthorizeFolder("/TargetGroups")，則需要加上此屬性
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // 用於顯示目標群組的詳細資料
        public TargetGroup TargetGroup { get; set; } = default!;

        // 當頁面以 GET 方式請求時執行，需要傳入 id 參數
        public async Task<IActionResult> OnGetAsync(int? id)
        {
            // 如果沒有提供 id，返回 NotFound
            if (id == null)
            {
                return NotFound();
            }

            // 從資料庫非同步查詢具有指定 id 的 TargetGroup
            // 使用 FirstOrDefaultAsync，如果找不到會返回 null
            var targetgroup = await _context.TargetGroups.FirstOrDefaultAsync(m => m.Id == id);

            // 如果在資料庫中找不到對應的群組，返回 NotFound
            if (targetgroup == null)
            {
                return NotFound();
            }
            else
            {
                // 將查詢到的群組資料賦值給頁面模型屬性
                TargetGroup = targetgroup;
            }
            // 返回頁面
            return Page();
        }
    }
}