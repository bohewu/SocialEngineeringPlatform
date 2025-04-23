using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SocialEngineeringPlatform.Web.Data;
using SocialEngineeringPlatform.Web.Models.Core;
using SocialEngineeringPlatform.Web.Models.Enums; // For CampaignStatus

namespace SocialEngineeringPlatform.Web.Pages.Campaigns
{
    // [Authorize]
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IBackgroundJobClient _backgroundJobClient; // *** 注入 Hangfire Client ***
        private readonly ILogger<DeleteModel> _logger;

        public DeleteModel(ApplicationDbContext context, IBackgroundJobClient backgroundJobClient,
            ILogger<DeleteModel> logger)
        {
            _context = context;
            _backgroundJobClient = backgroundJobClient;
            _logger = logger;
        }

        [BindProperty] public Campaign Campaign { get; set; } = default!;

        // 用於在 View 中判斷是否能刪除
        public bool CanDelete { get; set; } = false; // 預設不可刪除
        public string DeletionWarningMessage { get; set; } = "無法刪除此活動。"; // 預設警告訊息

        // 檢查活動是否可被刪除
        private async Task<bool> CheckIfDeletable(int id)
        {
            // 1. 檢查狀態是否為 Draft 或 Cancelled
            var campaignStatus = await _context.Campaigns
                .Where(c => c.Id == id)
                .Select(c => c.Status)
                .FirstOrDefaultAsync();

            if (campaignStatus != CampaignStatus.Draft && campaignStatus != CampaignStatus.Cancelled)
            {
                DeletionWarningMessage = "無法刪除：只有處於「草稿」或「已取消」狀態的活動才能被刪除。";
                return false;
            }

            // 2. 檢查是否有相關聯的記錄 (Targets, Events, Logs)
            bool hasTargets = await _context.CampaignTargets.AnyAsync(ct => ct.CampaignId == id);
            if (hasTargets)
            {
                DeletionWarningMessage = "無法刪除：此活動已設定目標對象。請先移除所有目標對象。";
                return false;
            }

            bool hasEvents = await _context.TrackingEvents.AnyAsync(te => te.CampaignId == id);
            if (hasEvents)
            {
                DeletionWarningMessage = "無法刪除：此活動已有追蹤記錄。";
                return false;
            }

            bool hasLogs = await _context.MailSendLogs.AnyAsync(ml => ml.CampaignId == id);
            if (hasLogs)
            {
                DeletionWarningMessage = "無法刪除：此活動已有郵件發送記錄。";
                return false;
            }

            // 如果所有檢查都通過
            DeletionWarningMessage = "您確定要刪除這個演練活動嗎？此操作無法復原。";
            return true;
        }


        // OnGet 用於顯示要刪除的資料供確認
        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // *** 修改：查詢時包含 JobId ***
            var campaign = await _context.Campaigns
                .Include(c => c.MailTemplate)
                .Include(c => c.LandingPageTemplate)
                .Include(c => c.TargetGroup)
                .Include(c => c.CreatedByUser)
                .AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
            if (campaign == null)
            {
                return NotFound();
            }

            Campaign = campaign;
            CanDelete = await CheckIfDeletable(id.Value); // 執行可刪除檢查
            return Page();
        }

        // OnPost 用於處理刪除確認後的提交
        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // *** 再次檢查是否可刪除 ***
            CanDelete = await CheckIfDeletable(id.Value);
            if (!CanDelete)
            {
                // 如果不可刪除，設定錯誤訊息並返回頁面
                ModelState.AddModelError(string.Empty, DeletionWarningMessage);
                // 需要重新載入資料以顯示頁面
                var campaign = await _context.Campaigns
                    .Include(c => c.MailTemplate)
                    .Include(c => c.LandingPageTemplate)
                    .Include(c => c.CreatedByUser)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.Id == id);
                if (campaign != null) Campaign = campaign;
                return Page();
            }

            // 查找要刪除的活動
            var campaignToDelete = await _context.Campaigns.FindAsync(id);

            if (campaignToDelete != null)
            {
                // *** 修改：使用 campaignToDelete.JobId 取消關聯的 Hangfire Job ***
                if (!string.IsNullOrWhiteSpace(campaignToDelete.JobId))
                {
                    _logger.LogInformation("正在取消活動 ID: {CampaignId} 的排程工作 JobId: {JobId}", id, campaignToDelete.JobId);
                    _backgroundJobClient.Delete(campaignToDelete.JobId);
                }
                // *** 取消 Job 結束 ***
                
                // 確認可刪除後，執行刪除
                _context.Campaigns.Remove(campaignToDelete);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "演練活動已成功刪除！";
            }
            // else: 找不到表示已被刪除

            return RedirectToPage("./Index");
        }
    }
}