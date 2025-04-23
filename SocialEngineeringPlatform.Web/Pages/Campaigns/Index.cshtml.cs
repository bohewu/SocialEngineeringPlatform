using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SocialEngineeringPlatform.Web.Data;
using SocialEngineeringPlatform.Web.Models.Core;

namespace SocialEngineeringPlatform.Web.Pages.Campaigns
{
    // [Authorize] // 根據需要啟用授權
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Campaign> CampaignList { get;set; } = default!;

        public async Task OnGetAsync()
        {
            // 查詢活動列表，並載入相關的範本和建立者資訊
            CampaignList = await _context.Campaigns
                .Include(c => c.MailTemplate) // 載入郵件範本
                .Include(c => c.LandingPageTemplate) // 載入登陸頁範本
                .Include(c => c.CreatedByUser) // 載入建立者
                .OrderByDescending(c => c.CreateTime) // 依照建立時間排序 (新的在前)
                .AsNoTracking() // 列表頁通常不需要追蹤變更
                .ToListAsync();
        }
    }
}