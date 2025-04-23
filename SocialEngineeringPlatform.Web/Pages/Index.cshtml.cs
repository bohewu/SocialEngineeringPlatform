using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SocialEngineeringPlatform.Web.Data;
using SocialEngineeringPlatform.Web.Models.Enums;
using SocialEngineeringPlatform.Web.ViewModels; // *** 引用 ViewModel ***
using Microsoft.AspNetCore.Authorization; // 加入授權

namespace SocialEngineeringPlatform.Web.Pages
{
    [Authorize] // *** 儀表板通常需要登入才能訪問 ***
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ApplicationDbContext context, ILogger<IndexModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        // 綁定 ViewModel 到頁面
        [BindProperty]
        public DashboardViewModel DashboardData { get; set; } = new DashboardViewModel();

        public async Task OnGetAsync()
        {
            try
            {
                // 查詢總活動數
                DashboardData.TotalCampaigns = await _context.Campaigns.CountAsync();

                // 查詢各狀態活動數
                DashboardData.CampaignStatusCounts = await _context.Campaigns
                    .GroupBy(c => c.Status)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Status, x => x.Count);

                // 查詢最近 5 筆建立的活動 (包含建立者資訊)
                DashboardData.RecentCampaigns = await _context.Campaigns
                    .Include(c => c.CreatedByUser) // 載入建立者
                    .OrderByDescending(c => c.CreateTime)
                    .Take(5) // 只取最近 5 筆
                    .AsNoTracking()
                    .ToListAsync();

                // 確保所有狀態都有計數 (即使是 0)
                foreach (CampaignStatus status in Enum.GetValues(typeof(CampaignStatus)))
                {
                    if (!DashboardData.CampaignStatusCounts.ContainsKey(status))
                    {
                        DashboardData.CampaignStatusCounts.Add(status, 0);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "載入儀表板資料時發生錯誤。");
                // 可以設定錯誤訊息顯示給使用者
                // TempData["ErrorMessage"] = "載入儀表板資料失敗。";
            }
        }
    }
}