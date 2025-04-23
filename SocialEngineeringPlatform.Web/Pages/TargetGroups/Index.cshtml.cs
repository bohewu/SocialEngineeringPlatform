using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SocialEngineeringPlatform.Web.Data;
using SocialEngineeringPlatform.Web.Models.Core;

// 加入授權

namespace SocialEngineeringPlatform.Web.Pages.TargetGroups
{
    // [Authorize] // 如果尚未在 Program.cs 中設定 AuthorizeFolder("/TargetGroups")，則需要加上此屬性
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // 用於在頁面上顯示的目標群組列表
        public IList<TargetGroup> TargetGroups { get; set; } = default!;

        // 當頁面以 GET 方式請求時執行
        public async Task OnGetAsync()
        {
            // 從資料庫非同步取得所有 TargetGroup 資料
            TargetGroups = await _context.TargetGroups.ToListAsync();
        }
    }
}