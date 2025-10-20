using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SocialEngineeringPlatform.Web.Data;
using SocialEngineeringPlatform.Web.Models.Core;
using SocialEngineeringPlatform.Web.Models.Enums;

// For Encoding

// For AnyAsync, FirstOrDefaultAsync

namespace SocialEngineeringPlatform.Web.Pages
{
    // [AllowAnonymous]
    public class TrackClickModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TrackClickModel> _logger;

        public TrackClickModel(ApplicationDbContext context, ILogger<TrackClickModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> OnGetAsync(
            [FromQuery(Name = "c")] int? campaignId,
            [FromQuery(Name = "t")] int? targetUserId,
            [FromQuery(Name = "url")] string? encodedUrl)
        {
            // 1. 驗證基本參數
            if (campaignId == null || targetUserId == null || string.IsNullOrWhiteSpace(encodedUrl))
            {
                _logger.LogWarning("點擊追蹤請求缺少必要參數。CampaignId: {Cid}, TargetUserId: {Tid}, EncodedUrl: {Url}", campaignId,
                    targetUserId, encodedUrl);
                // 即使參數錯誤，也嘗試導向到一個預設頁面或顯示錯誤，避免停在空白頁
                return RedirectToPage("/Error", new { message = "Invalid tracking link." });
                // return BadRequest("缺少追蹤參數。");
            }

            // 2. 解碼原始 URL
            string originalUrl;
            try
            {
                string base64 = encodedUrl.Replace('-', '+').Replace('_', '/');
                base64 = base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=');
                byte[] urlBytes = Convert.FromBase64String(base64);
                originalUrl = Encoding.UTF8.GetString(urlBytes);
                if (!Uri.TryCreate(originalUrl, UriKind.Absolute, out _))
                {
                    _logger.LogWarning("點擊追蹤請求中的 URL 解碼後無效。EncodedUrl: {EncodedUrl}", encodedUrl);
                    // 導向到錯誤頁或首頁
                    return RedirectToPage("/Error", new { message = "Invalid target URL." });
                    // return BadRequest("無效的目標 URL。");
                }
            }
            catch (Exception ex) // 捕捉所有解碼相關錯誤
            {
                _logger.LogError(ex, "點擊追蹤請求中的 URL 解碼時發生錯誤: {EncodedUrl}", encodedUrl);
                // 導向到錯誤頁或首頁
                return RedirectToPage("/Error", new { message = "URL decoding error." });
                // return BadRequest("URL 解碼時發生錯誤。");
            }

            // 3. 查詢活動狀態並驗證使用者
            // *** 修改：查詢時包含狀態，且不再 AsNoTracking ***
            var campaign = await _context.Campaigns
                .Select(c => new { c.Id, c.Status }) // 只選擇需要的欄位
                .FirstOrDefaultAsync(c => c.Id == campaignId.Value);
            bool targetUserExists = await _context.TargetUsers.AnyAsync(u => u.Id == targetUserId.Value);

            if (campaign == null || !targetUserExists)
            {
                _logger.LogWarning("點擊追蹤請求中的 CampaignId ({Cid}) 或 TargetUserId ({Tid}) 在資料庫中不存在。", campaignId.Value,
                    targetUserId.Value);
                // 即使找不到，仍然重新導向到原始 URL，避免影響使用者
                return Redirect(originalUrl);
                // return NotFound("指定的活動或目標不存在。");
            }

            // *** 新增：檢查活動狀態是否為 Running ***
            if (campaign.Status != CampaignStatus.Running)
            {
                _logger.LogInformation("點擊追蹤請求被忽略，因為活動 CampaignId={Cid} 的狀態為 {Status} (非 Running)。", campaignId.Value,
                    campaign.Status);
                // 狀態不符，直接重新導向，不記錄事件
                return Redirect(originalUrl);
            }

            // 4. 記錄追蹤事件 (僅在狀態為 Running 時)
            try
            {
                // 只記錄 URL 的 scheme 和 host，避免完整 URL 的日誌注入風險
                var urlInfo = Uri.TryCreate(originalUrl, UriKind.Absolute, out var parsedUri) 
                    ? $"{parsedUri.Scheme}://{parsedUri.Host}" 
                    : "invalid-url";
                _logger.LogInformation("記錄點擊事件：CampaignId={Cid}, TargetUserId={Tid}, UrlHost={UrlHost}",
                    campaignId.Value, targetUserId.Value, urlInfo);
                var trackingEvent = new TrackingEvent
                {
                    CampaignId = campaignId.Value,
                    TargetUserId = targetUserId.Value,
                    EventType = TrackingEventType.Clicked,
                    EventTime = DateTime.UtcNow,
                    EventDetails = originalUrl
                };
                _context.TrackingEvents.Add(trackingEvent);
                await _context.SaveChangesAsync();
                _logger.LogInformation("點擊事件記錄成功。");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "記錄點擊事件時發生資料庫錯誤。CampaignId={Cid}, TargetUserId={Tid}", campaignId.Value,
                    targetUserId.Value);
                // 即使記錄失敗，仍然重新導向
            }

            // 5. 重新導向到原始 URL
            _logger.LogInformation("重新導向使用者至: {OriginalUrl}", originalUrl);
            return Redirect(originalUrl);
        }
    }
}