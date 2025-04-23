using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore; // 需要引用 EntityFrameworkCore
using SocialEngineeringPlatform.Web.Data;
using SocialEngineeringPlatform.Web.Models.Core;

// 加入授權

namespace SocialEngineeringPlatform.Web.Pages.LandingPageTemplates
{
    // [Authorize] // 如果需要，加上授權限制
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<LandingPageTemplate> LandingPageTemplate { get;set; } = default!;

        public async Task OnGetAsync()
        {
            // 查詢登陸頁範本，並載入建立者資訊
            LandingPageTemplate = await _context.LandingPageTemplates
                .Include(l => l.CreatedByUser) // 載入建立者
                .ToListAsync();
        }
    }
}