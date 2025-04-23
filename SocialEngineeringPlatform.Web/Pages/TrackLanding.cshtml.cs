using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SocialEngineeringPlatform.Web.Data;
using SocialEngineeringPlatform.Web.Models.Core;
using SocialEngineeringPlatform.Web.Models.Enums;
using AngleSharp; // *** 加入 AngleSharp using ***
using AngleSharp.Html.Dom;
namespace SocialEngineeringPlatform.Web.Pages
{
    // [AllowAnonymous] // 通常登陸頁是公開訪問的
    [IgnoreAntiforgeryToken]
    public class TrackLandingModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TrackLandingModel> _logger;

        public TrackLandingModel(ApplicationDbContext context, ILogger<TrackLandingModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        // OnGet 處理登陸頁連結的請求
        public async Task<IActionResult> OnGetAsync(
            [FromQuery(Name = "c")] int? campaignId,
            [FromQuery(Name = "t")] int? targetUserId)
        {
            // 1. 驗證基本參數
            if (campaignId == null || targetUserId == null)
            {
                _logger.LogWarning("登陸頁追蹤請求缺少必要參數。CampaignId: {Cid}, TargetUserId: {Tid}", campaignId, targetUserId);
                return BadRequest("缺少追蹤參數。");
            }

            // 2. 查詢活動及其關聯的登陸頁範本 ID
            var campaignInfo = await _context.Campaigns
                                     .Where(c => c.Id == campaignId.Value)
                                     .Select(c => new { c.Status, c.LandingPageTemplateId }) // 只選擇需要的欄位
                                     .FirstOrDefaultAsync();

            bool targetUserExists = await _context.TargetUsers.AnyAsync(u => u.Id == targetUserId.Value);

            if (campaignInfo == null || !targetUserExists)
            {
                 _logger.LogWarning("登陸頁追蹤請求中的 CampaignId ({Cid}) 或 TargetUserId ({Tid}) 在資料庫中不存在。", campaignId.Value, targetUserId.Value);
                 // 找不到活動或使用者，可以顯示一個通用的錯誤頁面或導向首頁
                 return RedirectToPage("/Error", new { message = "無效的連結。" });
                 // return NotFound("指定的活動或目標不存在。");
            }

            // 3. 檢查活動狀態是否為 Running (只有運行中才記錄點擊並顯示登陸頁)
            if (campaignInfo.Status != CampaignStatus.Running)
            {
                 _logger.LogInformation("登陸頁追蹤請求被忽略，因為活動 CampaignId={Cid} 的狀態為 {Status} (非 Running)。", campaignId.Value, campaignInfo.Status);
                 // 可以顯示一個提示頁面告知活動已結束或未開始
                 return Content("此活動目前不在進行中。", "text/plain", System.Text.Encoding.UTF8);
                 // 或者導向到錯誤頁
                 // return RedirectToPage("/Error", new { message = "活動不在進行中。" });
            }

            // 4. 檢查活動是否關聯了登陸頁範本
            if (!campaignInfo.LandingPageTemplateId.HasValue)
            {
                 _logger.LogWarning("活動 CampaignId={Cid} 未設定登陸頁範本，但收到了登陸頁追蹤請求。", campaignId.Value);
                 // 沒有設定登陸頁，可以導向到一個預設頁面或錯誤頁
                 return RedirectToPage("/Error", new { message = "此活動未設定登陸頁。" });
            }


            // 5. 記錄點擊事件 (Clicked)
            try
            {
                 _logger.LogInformation("記錄登陸頁點擊事件：CampaignId={Cid}, TargetUserId={Tid}", campaignId.Value, targetUserId.Value);
                 var trackingEvent = new TrackingEvent
                 {
                     CampaignId = campaignId.Value,
                     TargetUserId = targetUserId.Value,
                     EventType = TrackingEventType.Clicked, // 登陸頁訪問視為一次點擊
                     EventTime = DateTime.UtcNow,
                     EventDetails = $"Landing Page Visit (TemplateId: {campaignInfo.LandingPageTemplateId.Value})" // 記錄是訪問登陸頁
                 };
                 _context.TrackingEvents.Add(trackingEvent);
                 await _context.SaveChangesAsync();
                 _logger.LogInformation("登陸頁點擊事件記錄成功。");
            }
            catch(Exception ex)
            {
                 _logger.LogError(ex, "記錄登陸頁點擊事件時發生資料庫錯誤。CampaignId={Cid}, TargetUserId={Tid}", campaignId.Value, targetUserId.Value);
                 // 即使記錄失敗，仍然嘗試顯示登陸頁
            }

            // 6. 查詢登陸頁範本的 HTML 內容
            var landingPageHtml = await _context.LandingPageTemplates
                                        .Where(lp => lp.Id == campaignInfo.LandingPageTemplateId.Value)
                                        .Select(lp => lp.HtmlContent)
                                        .FirstOrDefaultAsync();

            if (landingPageHtml == null)
            {
                 _logger.LogError("找不到活動 CampaignId={Cid} 對應的登陸頁範本 (ID: {LpId}) 的 HTML 內容。", campaignId.Value, campaignInfo.LandingPageTemplateId.Value);
                 return RedirectToPage("/Error", new { message = "無法載入登陸頁內容。" });
            }

            // *** 7. 修改 HTML，注入隱藏欄位 ***
            string modifiedHtml;
            try
            {
                var config = Configuration.Default;
                var context = BrowsingContext.New(config);
                var document = await context.OpenAsync(req => req.Content(landingPageHtml));

                // 找到頁面中的第一個表單 (可以根據需要修改選擇器)
                var formEl = document.QuerySelector("form");

                if (formEl is IHtmlFormElement formElement)
                {
                    // 設定表單的 action 指向提交端點
                    formElement.Action = "/Track/Submit"; // 設定提交目標 URL
                    formElement.Method = "POST"; // 確保是 POST

                    // 建立並附加 CampaignId 隱藏欄位
                    var campaignIdInput = document.CreateElement("input") as IHtmlInputElement;
                    campaignIdInput!.Type = "hidden";
                    campaignIdInput.Name = "CampaignId"; // 與 TrackSubmitModel 的 BindProperty 對應
                    campaignIdInput.SetAttribute("value", campaignId.Value.ToString());
                    formElement.AppendChild(campaignIdInput);

                     // 建立並附加 TargetUserId 隱藏欄位
                    var targetUserIdInput = document.CreateElement("input") as IHtmlInputElement;
                    targetUserIdInput!.Type = "hidden";
                    targetUserIdInput.Name = "TargetUserId"; // 與 TrackSubmitModel 的 BindProperty 對應
                    targetUserIdInput.SetAttribute("value", targetUserId.Value.ToString());
                    formElement.AppendChild(targetUserIdInput);

                     _logger.LogInformation("已將追蹤參數注入登陸頁表單。CampaignId={Cid}, TargetUserId={Tid}", campaignId.Value, targetUserId.Value);
                }
                else
                {
                    _logger.LogWarning("在登陸頁範本 (ID: {LpId}) 中找不到 <form> 標籤，無法注入追蹤參數。", campaignInfo.LandingPageTemplateId.Value);
                    // 即使找不到表單，仍然顯示原始 HTML
                }

                // 取得修改後的 HTML
                modifiedHtml = document.ToHtml();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "處理登陸頁 HTML 以注入參數時發生錯誤。LandingPageTemplateId={LpId}", campaignInfo.LandingPageTemplateId.Value);
                modifiedHtml = landingPageHtml; // 如果處理失敗，返回原始 HTML
            }
            // *** HTML 修改結束 ***


            // 8. 返回修改後的 HTML 內容
            return new ContentResult
            {
                ContentType = "text/html; charset=utf-8", // *** 明確指定 UTF-8 編碼 ***
                Content = modifiedHtml
            };
        }
    }
}