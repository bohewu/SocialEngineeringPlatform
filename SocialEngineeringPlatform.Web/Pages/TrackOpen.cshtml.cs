using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SocialEngineeringPlatform.Web.Data;
using SocialEngineeringPlatform.Web.Models.Core;
using SocialEngineeringPlatform.Web.Models.Enums;
using Microsoft.EntityFrameworkCore; // For AnyAsync, FirstOrDefaultAsync

namespace SocialEngineeringPlatform.Web.Pages
{
    // [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public class TrackOpenModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TrackOpenModel> _logger;

        public TrackOpenModel(ApplicationDbContext context, ILogger<TrackOpenModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> OnGetAsync(
            [FromQuery(Name = "c")] int? campaignId,
            [FromQuery(Name = "t")] int? targetUserId)
        {
            // 1. 驗證基本參數
            if (campaignId == null || targetUserId == null)
            {
                _logger.LogWarning("開啟追蹤請求缺少必要參數。CampaignId: {Cid}, TargetUserId: {Tid}", campaignId, targetUserId);
                return new NoContentResult(); // 返回空響應
            }

            // 2. 查詢活動狀態並驗證使用者
            // *** 修改：查詢時包含狀態，且不再 AsNoTracking ***
            var campaign = await _context.Campaigns
                .Select(c => new { c.Id, c.Status }) // 只選擇需要的欄位
                .FirstOrDefaultAsync(c => c.Id == campaignId.Value);
            bool targetUserExists = await _context.TargetUsers.AnyAsync(u => u.Id == targetUserId.Value);

            if (campaign == null || !targetUserExists)
            {
                _logger.LogWarning("開啟追蹤請求中的 CampaignId ({Cid}) 或 TargetUserId ({Tid}) 在資料庫中不存在。", campaignId.Value,
                    targetUserId.Value);
                // 即使找不到，仍然返回空響應
                return new NoContentResult(); // 返回空響應
            }

            // *** 新增：檢查活動狀態是否為 Running ***
            if (campaign.Status != CampaignStatus.Running)
            {
                _logger.LogInformation("開啟追蹤請求被忽略，因為活動 CampaignId={Cid} 的狀態為 {Status} (非 Running)。", campaignId.Value,
                    campaign.Status);
                // 狀態不符，直接返回空響應，不記錄事件
                return new NoContentResult(); // 返回空響應
            }


            // 3. 記錄追蹤事件 (Opened) (僅在狀態為 Running 時)
            try
            {
                _logger.LogInformation("記錄開啟事件：CampaignId={Cid}, TargetUserId={Tid}", campaignId.Value,
                    targetUserId.Value);
                var trackingEvent = new TrackingEvent
                {
                    CampaignId = campaignId.Value,
                    TargetUserId = targetUserId.Value,
                    EventType = TrackingEventType.Opened,
                    EventTime = DateTime.UtcNow,
                    EventDetails = $"UserAgent: {Request.Headers["User-Agent"]}"
                };
                _context.TrackingEvents.Add(trackingEvent);
                await _context.SaveChangesAsync();
                _logger.LogInformation("開啟事件記錄成功。");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "記錄開啟事件時發生資料庫錯誤。CampaignId={Cid}, TargetUserId={Tid}", campaignId.Value,
                    targetUserId.Value);
                // 即使記錄失敗，也要返回正常響應
            }

            // 4. 返回 HTTP 204 No Content
            return new NoContentResult(); // 或返回 1x1 GIF
        }
    }
}