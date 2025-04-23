using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering; // For SelectList, SelectListItem
using Microsoft.EntityFrameworkCore;
using SocialEngineeringPlatform.Web.Data;
using SocialEngineeringPlatform.Web.Models.Core;
using SocialEngineeringPlatform.Web.Models.Enums; // For CampaignStatus
using Microsoft.AspNetCore.Identity; // For UserManager (Optional)
using Microsoft.Extensions.Options;
using SocialEngineeringPlatform.Web.Models.Configuration;
using SocialEngineeringPlatform.Web.Services.Interfaces;

namespace SocialEngineeringPlatform.Web.Pages.Campaigns
{
    // [Authorize]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager; // 如果需要顯示建立者
        private readonly IBackgroundJobClient _backgroundJobClient; // *** 注入 Hangfire Client ***
        private readonly string _baseUrl;
        private readonly ILogger<EditModel> _logger; // *** 注入 Logger ***

        public EditModel(ApplicationDbContext context, IBackgroundJobClient backgroundJobClient,
            IOptions<AppSettings> appSettings, ILogger<EditModel> logger,
            UserManager<ApplicationUser> userManager) //, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _backgroundJobClient = backgroundJobClient;
            _logger = logger;
            _userManager = userManager;
            _logger = logger;
            _baseUrl = appSettings.Value?.BaseUrl ?? "https://localhost:7190"; // 確保有 BaseUrl
        }

        [BindProperty] public Campaign Campaign { get; set; } = default!;

        // 下拉選單選項
        public SelectList? MailTemplateSL { get; set; }
        public SelectList? LandingPageTemplateSL { get; set; }
        public SelectList? StatusSL { get; set; } // 狀態下拉選單

        public SelectList? TargetGroupSL { get; set; } // *** 新增目標群組下拉選單 ***

        // --- 輔助方法：準備下拉選單資料 ---
        private async Task PopulateMailTemplatesDropDownListAsync(ApplicationDbContext context,
            object? selectedTemplate = null)
        {
            var templatesQuery = from t in context.MailTemplates orderby t.Name select new { t.Id, t.Name };
            MailTemplateSL = new SelectList(await templatesQuery.AsNoTracking().ToListAsync(), "Id", "Name",
                selectedTemplate);
        }

        private async Task PopulateLandingPageTemplatesDropDownListAsync(ApplicationDbContext context,
            object? selectedTemplate = null)
        {
            var templatesQuery = from t in context.LandingPageTemplates orderby t.Name select new { t.Id, t.Name };
            LandingPageTemplateSL = new SelectList(await templatesQuery.AsNoTracking().ToListAsync(), "Id", "Name",
                selectedTemplate);
        }

        private void PopulateStatusDropDownList(object? selectedStatus = null)
        {
            // 從 Enum 取得所有狀態選項
            // 使用 EnumHelper 或直接轉換
            var statusOptions = Enum.GetValues(typeof(CampaignStatus))
                .Cast<CampaignStatus>()
                .Select(status => new SelectListItem
                {
                    Value = status.ToString(), // 值是 Enum 的字串表示
                    Text = status.ToString() // 顯示文字也是 Enum 的字串表示 (未來可做多語系)
                }).ToList();

            StatusSL = new SelectList(statusOptions, "Value", "Text", selectedStatus?.ToString());
        }

        private async Task PopulateTargetGroupsDropDownListAsync(ApplicationDbContext context,
            object? selectedGroup = null)
        {
            var groupsQuery = from g in context.TargetGroups orderby g.Name select new { g.Id, g.Name };
            TargetGroupSL = new SelectList(await groupsQuery.AsNoTracking().ToListAsync(), "Id", "Name", selectedGroup);
        }
        // --- 輔助方法結束 ---

        // OnGet 用於載入要編輯的資料和下拉選單
        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var campaign = await _context.Campaigns
                .Include(c => c.CreatedByUser) // 可選
                .FirstOrDefaultAsync(m => m.Id == id);
            if (campaign == null)
            {
                return NotFound();
            }

            Campaign = campaign;
            // 載入所有下拉選單
            await RepopulateAllDropDownListsAsync();
            return Page();
        }

        // OnPost 用於處理編輯表單的提交
        public async Task<IActionResult> OnPostAsync()
        {
            // 移除不需要驗證的導覽屬性及後端設定欄位
            ModelState.Remove("Campaign.MailTemplate");
            ModelState.Remove("Campaign.LandingPageTemplate");
            ModelState.Remove("Campaign.TargetGroup");
            ModelState.Remove("Campaign.CreatedByUser");
            ModelState.Remove("Campaign.CampaignTargets");
            ModelState.Remove("Campaign.TrackingEvents");
            ModelState.Remove("Campaign.MailSendLogs");
            ModelState.Remove("Campaign.CreatedByUserId"); // 不可修改
            ModelState.Remove("Campaign.CreateTime"); // 不可修改
            ModelState.Remove("Campaign.ActualStartTime"); // 由系統控制
            ModelState.Remove("Campaign.EndTime"); // 由系統控制
            ModelState.Remove("Campaign.JobId"); // *** 移除 JobId 驗證 ***

            if (!ModelState.IsValid)
            {
                // *** 如果驗證失敗，需要重新載入下拉選單資料 ***
                await RepopulateAllDropDownListsAsync();
                return Page();
            }

            // *** 取得編輯前的 JobId ***
            var campaignBeforeEdit = await _context.Campaigns
                .Select(c => new { c.Id, c.Status, c.IsAutoSend, c.ScheduledSendTime, c.JobId }) // 讀取 JobId
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == Campaign.Id);
            if (campaignBeforeEdit == null)
            {
                return NotFound();
            }

            string? oldJobId = campaignBeforeEdit.JobId; // 取得舊的 JobId

            // --- Fetch-Then-Update ---
            var campaignToUpdate = await _context.Campaigns.FindAsync(Campaign.Id);

            if (campaignToUpdate == null)
            {
                return NotFound(); // 資料已被刪除
            }

            // *** 處理舊的 Hangfire Job (如果需要取消) ***
            bool shouldCancelOldJob = false;
            // (判斷邏輯同前) ...
            if (campaignBeforeEdit.Status == CampaignStatus.Scheduled && Campaign.Status != CampaignStatus.Scheduled)
            {
                shouldCancelOldJob = true;
            }
            else if (campaignBeforeEdit.IsAutoSend && !Campaign.IsAutoSend ||
                     campaignBeforeEdit.ScheduledSendTime.HasValue && !Campaign.ScheduledSendTime.HasValue ||
                     campaignBeforeEdit.ScheduledSendTime.HasValue && Campaign.ScheduledSendTime.HasValue &&
                     campaignBeforeEdit.ScheduledSendTime.Value != Campaign.ScheduledSendTime.Value)
            {
                shouldCancelOldJob = true;
            }

            if (shouldCancelOldJob && !string.IsNullOrWhiteSpace(oldJobId))
            {
                _logger.LogInformation("正在取消舊的排程工作 JobId: {JobId} for CampaignId: {CampaignId}", oldJobId, Campaign.Id);
                _backgroundJobClient.Delete(oldJobId); // 取消舊的工作
                campaignToUpdate.JobId = null; // *** 清除 JobId 記錄 ***
            }
            // *** 舊 Job 處理結束 ***

            // 使用 TryUpdateModelAsync 更新允許修改的欄位
            // 注意：不要包含 CreateTime, CreatedByUserId, ActualStartTime, EndTime
            if (await TryUpdateModelAsync<Campaign>(
                    campaignToUpdate,
                    "Campaign", // Prefix for form fields
                    c => c.Name, c => c.Description, c => c.MailTemplateId, c => c.LandingPageTemplateId,
                    c => c.TargetGroupId, // <-- 加入 TargetGroupId
                    c => c.ScheduledSendTime, c => c.Status, c => c.IsAutoSend, c => c.SendBatchDelaySeconds,
                    c => c.TrackOpens))
            {
                campaignToUpdate.UpdateTime = DateTime.UtcNow; // 更新修改時間

                // *** 處理新的 Hangfire Job (如果需要排程) ***
                string? newJobId;
                if (campaignToUpdate is
                    {
                        Status: CampaignStatus.Scheduled,
                        IsAutoSend: true,
                        ScheduledSendTime: not null
                    }
                    && campaignToUpdate.ScheduledSendTime.Value > DateTime.UtcNow)
                {
                    try
                    {
                        newJobId = _backgroundJobClient.Schedule<ICampaignExecutionService>(
                            service => service.ExecuteCampaignAsync(campaignToUpdate.Id, _baseUrl,
                                CancellationToken.None),
                            campaignToUpdate.ScheduledSendTime.Value);
                        _logger.LogInformation("已為活動 ID: {CampaignId} 更新/排定自動發送工作 JobId: {JobId}...",
                            campaignToUpdate.Id, newJobId);
                        campaignToUpdate.JobId = newJobId; // *** 記錄新的 JobId ***
                    }
                    catch (Exception ex)
                    {
                        /* ... 錯誤處理，狀態改回 Draft ... */
                        campaignToUpdate.Status = CampaignStatus.Draft;
                        campaignToUpdate.JobId = null;
                    }
                }
                // 如果取消了舊的且沒有排新的，確保 JobId 為 null (已在取消邏輯中處理)
                // else if (shouldCancelOldJob && string.IsNullOrWhiteSpace(newJobId))
                // {
                //      campaignToUpdate.JobId = null;
                // }
                // *** 新 Job 處理結束 ***

                try
                {
                    await _context.SaveChangesAsync(); // 儲存變更
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CampaignExists(Campaign.Id))
                    {
                        return NotFound();
                    }

                    ModelState.AddModelError(string.Empty, "此資料已被其他人修改，請重新載入後再試。");
                    // *** 重新載入所有下拉選單 ***
                    await RepopulateAllDropDownListsAsync();
                    return Page();
                }

                return RedirectToPage("./Index"); // 成功後導向列表頁
            }

            // 如果 TryUpdateModelAsync 失敗，重新載入下拉選單並返回頁面
            await RepopulateAllDropDownListsAsync();
            return Page();
        }

        private bool CampaignExists(int id)
        {
            return _context.Campaigns.Any(e => e.Id == id);
        }

        private async Task RepopulateAllDropDownListsAsync()
        {
            await PopulateMailTemplatesDropDownListAsync(_context, Campaign.MailTemplateId);
            await PopulateLandingPageTemplatesDropDownListAsync(_context, Campaign.LandingPageTemplateId);
            await PopulateTargetGroupsDropDownListAsync(_context, Campaign.TargetGroupId);
            PopulateStatusDropDownList(Campaign.Status);
        }
    }
}