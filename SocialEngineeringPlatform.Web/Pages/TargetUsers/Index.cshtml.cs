using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore; // 需要引用 EntityFrameworkCore
using SocialEngineeringPlatform.Web.Data;
using SocialEngineeringPlatform.Web.Models.Core;

// 加入授權

namespace SocialEngineeringPlatform.Web.Pages.TargetUsers
{
    // [Authorize] // 如果需要，加上授權限制
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<TargetUser> TargetUser { get;set; } = default!;

        public async Task OnGetAsync()
        {
            // 從資料庫取得目標使用者資料
            // 使用 .Include(t => t.Group) 來預先載入關聯的 TargetGroup 資料
            TargetUser = await _context.TargetUsers
                .Include(t => t.Group).ToListAsync();
        }
    }
}