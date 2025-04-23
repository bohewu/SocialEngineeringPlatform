// For Encoding
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SocialEngineeringPlatform.Web.Data;
using SocialEngineeringPlatform.Web.Models.Core;
using SocialEngineeringPlatform.Web.Models.Enums; // For Enums
using SocialEngineeringPlatform.Web.Services.Interfaces; // For IMailService, ICampaignExecutionService
// For ILogger
// using Microsoft.AspNetCore.Routing; // 不再需要 LinkGenerator
using SocialEngineeringPlatform.Web.ViewModels; // For CampaignExecutionResult

namespace SocialEngineeringPlatform.Web.Pages.Campaigns
{
    // [Authorize]
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        // private readonly IMailService _mailService; // *** 不再直接需要 MailService ***
        private readonly ILogger<DetailsModel> _logger;
        private readonly ICampaignExecutionService _campaignExecutionService; // *** 注入活動執行服務 ***

        public DetailsModel(
            ApplicationDbContext context,
            // IMailService mailService, // *** 移除 MailService 注入 ***
            ILogger<DetailsModel> logger,
            ICampaignExecutionService campaignExecutionService) // *** 加入建構子注入 ***
        {
            _context = context;
            // _mailService = mailService;
            _logger = logger;
            _campaignExecutionService = campaignExecutionService; // *** 初始化活動執行服務 ***
        }

        public Campaign Campaign { get; set; } = default!;

        // OnGet 用於顯示活動詳細資料
        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var campaign = await _context.Campaigns
                .Include(c => c.MailTemplate)
                .Include(c => c.LandingPageTemplate)
                .Include(c => c.TargetGroup) // *** 載入目標群組資訊 ***
                .Include(c => c.CreatedByUser)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);
            if (campaign == null)
            {
                return NotFound();
            }

            Campaign = campaign;
            return Page();
        }

        // 處理 "立即發送" 按鈕的 Handler
        public async Task<IActionResult> OnPostSendNowAsync(int id)
        {
            _logger.LogInformation("收到手動啟動活動 ID: {CampaignId} 的請求。", id);

            // *** 重構：呼叫 CampaignExecutionService ***
            string baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
            CampaignExecutionResult result =
                await _campaignExecutionService.ExecuteCampaignAsync(id, baseUrl, CancellationToken.None);

            // 根據執行結果設定 TempData
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                // 如果服務內部已有詳細錯誤，直接顯示；否則顯示通用錯誤
                TempData["ErrorMessage"] = string.IsNullOrWhiteSpace(result.Message)
                    ? "啟動或執行活動時發生錯誤。"
                    : result.Message;
            }
            // *** 重構結束 ***

            // 重新導向回詳細資料頁面以顯示訊息和更新後的狀態
            return RedirectToPage(new { id = id });
        }

        // 處理 "手動結束" 按鈕的 Handler
        public async Task<IActionResult> OnPostEndCampaignAsync(int id)
        {
            var campaignToEnd = await _context.Campaigns.FindAsync(id);
            if (campaignToEnd == null)
            {
                TempData["ErrorMessage"] = "找不到指定的活動。";
                return RedirectToPage("./Index");
            }

            if (campaignToEnd.Status == CampaignStatus.Running || campaignToEnd.Status == CampaignStatus.Scheduled)
            {
                campaignToEnd.Status = CampaignStatus.Completed;
                campaignToEnd.EndTime = DateTime.UtcNow;
                campaignToEnd.UpdateTime = DateTime.UtcNow;
                try
                {
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"活動 '{campaignToEnd.Name}' 已被手動結束。";
                }
                catch (DbUpdateConcurrencyException)
                {
                    TempData["ErrorMessage"] = "儲存時發生併發衝突...";
                    return RedirectToPage(new { id = id });
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"手動結束活動時發生錯誤：{ex.Message}";
                    _logger.LogError(ex, "手動結束活動 ID: {CampaignId} 時發生錯誤。", id);
                }
            }
            else
            {
                TempData["WarningMessage"] = $"活動狀態為 {campaignToEnd.Status}，無法手動結束。";
            }

            return RedirectToPage(new { id = id });
        }
    }
}