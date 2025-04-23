using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SocialEngineeringPlatform.Web.Data;
using SocialEngineeringPlatform.Web.Models.Core;
using SocialEngineeringPlatform.Web.Models.Enums;

namespace SocialEngineeringPlatform.Web.Pages
{
    // [AllowAnonymous] // 通常提交端點也是公開的
    [IgnoreAntiforgeryToken] // 因為表單是動態產生的，可能不易加入 Antiforgery Token
    public class TrackSubmitModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TrackSubmitModel> _logger;

        public TrackSubmitModel(ApplicationDbContext context, ILogger<TrackSubmitModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        // 從表單接收資料 (使用 BindProperty)
        // 名稱需要與登陸頁 HTML 中 input 的 name 屬性對應
        [BindProperty]
        public int? CampaignId { get; set; } // 從隱藏欄位接收

        [BindProperty]
        public int? TargetUserId { get; set; } // 從隱藏欄位接收

        // 可以選擇性地接收其他表單欄位名稱，以便記錄哪些欄位被提交了
        // 但 **絕對不要** 綁定或儲存密碼欄位！
        [BindProperty]
        public string? Username { get; set; } // 範例：接收使用者名稱欄位

        // 用於在頁面上顯示回應訊息
        public string ResponseMessage { get; set; } = "操作已記錄。"; // 預設訊息

        // OnPostAsync 處理表單提交
        public async Task<IActionResult> OnPostAsync()
        {
            // 1. 驗證基本追蹤參數
            if (CampaignId == null || TargetUserId == null)
            {
                _logger.LogWarning("表單提交追蹤請求缺少必要參數。CampaignId: {Cid}, TargetUserId: {Tid}", CampaignId, TargetUserId);
                // 可以顯示一個通用的錯誤訊息
                ResponseMessage = "提交處理失敗，缺少必要資訊。";
                return Page(); // 返回頁面顯示錯誤訊息
                // return BadRequest("缺少追蹤參數。");
            }

            // 2. 查詢活動狀態並驗證使用者
            var campaign = await _context.Campaigns
                                 .Select(c => new { c.Id, c.Status })
                                 .FirstOrDefaultAsync(c => c.Id == CampaignId.Value);
            bool targetUserExists = await _context.TargetUsers.AnyAsync(u => u.Id == TargetUserId.Value);

            if (campaign == null || !targetUserExists)
            {
                 _logger.LogWarning("表單提交追蹤請求中的 CampaignId ({Cid}) 或 TargetUserId ({Tid}) 在資料庫中不存在。", CampaignId.Value, TargetUserId.Value);
                 ResponseMessage = "提交處理失敗，無效的請求。";
                 return Page();
                 // return NotFound("指定的活動或目標不存在。");
            }

            // 3. 檢查活動狀態是否為 Running
            if (campaign.Status != CampaignStatus.Running)
            {
                 _logger.LogInformation("表單提交追蹤請求被忽略，因為活動 CampaignId={Cid} 的狀態為 {Status} (非 Running)。", CampaignId.Value, campaign.Status);
                 ResponseMessage = "此活動目前不在進行中，操作無法完成。";
                 return Page();
            }

            // 4. 記錄追蹤事件 (SubmittedData)
            try
            {
                 _logger.LogInformation("記錄表單提交事件：CampaignId={Cid}, TargetUserId={Tid}", CampaignId.Value, TargetUserId.Value);

                 // 收集提交的欄位名稱 (範例，不含密碼)
                 var submittedFields = new List<string>();
                 if (!string.IsNullOrWhiteSpace(Username)) submittedFields.Add("Username");
                 // if (Request.Form.ContainsKey("password")) submittedFields.Add("Password"); // 檢查是否有密碼欄位

                 var trackingEvent = new TrackingEvent
                 {
                     CampaignId = CampaignId.Value,
                     TargetUserId = TargetUserId.Value,
                     EventType = TrackingEventType.SubmittedData, // 事件類型為 SubmittedData
                     EventTime = DateTime.UtcNow,
                     EventDetails = $"Submitted Fields: {string.Join(", ", submittedFields)}" // 記錄提交了哪些欄位
                 };
                 _context.TrackingEvents.Add(trackingEvent);
                 await _context.SaveChangesAsync();
                 _logger.LogInformation("表單提交事件記錄成功。");

                 // 設定一個更友善的回應訊息
                 ResponseMessage = "您的請求已收到。"; // 或 "登入失敗，請檢查您的帳號密碼。" 等模擬訊息
            }
            catch(Exception ex)
            {
                 _logger.LogError(ex, "記錄表單提交事件時發生資料庫錯誤。CampaignId={Cid}, TargetUserId={Tid}", CampaignId.Value, TargetUserId.Value);
                 ResponseMessage = "處理您的請求時發生內部錯誤。";
                 // 即使記錄失敗，也要顯示一個頁面給使用者
            }

            // 5. 返回頁面，顯示回應訊息
            return Page();
        }
    }
}