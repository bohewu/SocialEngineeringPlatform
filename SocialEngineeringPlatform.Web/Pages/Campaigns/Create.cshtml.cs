using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SocialEngineeringPlatform.Web.Data;
using SocialEngineeringPlatform.Web.Models.Core;
using SocialEngineeringPlatform.Web.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SocialEngineeringPlatform.Web.Models.Configuration;
using SocialEngineeringPlatform.Web.Services.Interfaces;

namespace SocialEngineeringPlatform.Web.Pages.Campaigns
{
    // [Authorize]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IBackgroundJobClient _backgroundJobClient; // *** 注入 Hangfire Client ***
        private readonly string _baseUrl; // 用於排程工作
        private ILogger<CreateModel> _logger;
        public CreateModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IBackgroundJobClient backgroundJobClient,
            IOptions<AppSettings> appSettings, ILogger<CreateModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _backgroundJobClient = backgroundJobClient;
            _logger = logger;
            _baseUrl = appSettings.Value?.BaseUrl ?? "https://localhost:7190"; // 確保有 BaseUrl
        }

        // OnGet 用於顯示空白表單，並準備下拉選單
        public async Task<IActionResult> OnGetAsync()
        {
            await PopulateMailTemplatesDropDownListAsync(_context);
            await PopulateLandingPageTemplatesDropDownListAsync(_context);
            await PopulateTargetGroupsDropDownListAsync(_context); // *** 載入目標群組 ***
            Campaign = new Campaign
            {
                Status = CampaignStatus.Draft,
                TrackOpens = true,
                Name = string.Empty,
                CreatedByUserId = string.Empty,
            }; // 設定預設值
            return Page();
        }

        [BindProperty]
        public Campaign Campaign { get; set; } = default!;

        // 下拉選單選項
        public SelectList? MailTemplateSL { get; set; }
        public SelectList? LandingPageTemplateSL { get; set; }
        public SelectList? TargetGroupSL { get; set; } // *** 新增目標群組下拉選單 ***


        // --- 輔助方法：準備下拉選單資料 ---
        private async Task PopulateMailTemplatesDropDownListAsync(ApplicationDbContext context, object? selectedTemplate = null)
        {
            var templatesQuery = from t in context.MailTemplates orderby t.Name select new { t.Id, t.Name };
            MailTemplateSL = new SelectList(await templatesQuery.AsNoTracking().ToListAsync(), "Id", "Name", selectedTemplate);
        }

        private async Task PopulateLandingPageTemplatesDropDownListAsync(ApplicationDbContext context, object? selectedTemplate = null)
        {
            var templatesQuery = from t in context.LandingPageTemplates orderby t.Name select new { t.Id, t.Name };
            LandingPageTemplateSL = new SelectList(await templatesQuery.AsNoTracking().ToListAsync(), "Id", "Name", selectedTemplate);
        }

        // *** 新增：準備目標群組下拉選單 ***
        private async Task PopulateTargetGroupsDropDownListAsync(ApplicationDbContext context, object? selectedGroup = null)
        {
            var groupsQuery = from g in context.TargetGroups
                              orderby g.Name
                              select new { g.Id, g.Name }; // 只選取 Id 和 Name

            TargetGroupSL = new SelectList(await groupsQuery.AsNoTracking().ToListAsync(),
                                            "Id", "Name", selectedGroup);
        }
        // --- 輔助方法結束 ---


        // OnPost 用於處理表單提交
        public async Task<IActionResult> OnPostAsync()
        {
            // 移除不需要驗證的導覽屬性
            ModelState.Remove("Campaign.MailTemplate");
            ModelState.Remove("Campaign.LandingPageTemplate");
            ModelState.Remove("Campaign.TargetGroup"); // *** 移除 TargetGroup 導覽屬性 ***
            ModelState.Remove("Campaign.CreatedByUser");
            ModelState.Remove("Campaign.CampaignTargets");
            ModelState.Remove("Campaign.TrackingEvents");
            ModelState.Remove("Campaign.MailSendLogs");
            ModelState.Remove("Campaign.CreatedByUserId");
            ModelState.Remove("Campaign.Status");


            // 自動設定建立者 ID
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                ModelState.AddModelError(string.Empty, "無法取得目前使用者資訊。");
                // 重新載入下拉選單
                await PopulateMailTemplatesDropDownListAsync(_context, Campaign.MailTemplateId);
                await PopulateLandingPageTemplatesDropDownListAsync(_context, Campaign.LandingPageTemplateId);
                await PopulateTargetGroupsDropDownListAsync(_context, Campaign.TargetGroupId); // *** 重新載入 ***
                return Page();
            }
            Campaign.CreatedByUserId = userId;

            // 設定預設狀態為 Draft
            Campaign.Status = CampaignStatus.Draft;
            // 設定時間戳記
            Campaign.CreateTime = DateTime.UtcNow;
            Campaign.UpdateTime = DateTime.UtcNow;
            Campaign.ActualStartTime = null;
            Campaign.EndTime = null;
            Campaign.JobId = null; // *** 初始 JobId 為 null ***


            if (!ModelState.IsValid)
            {
                // 如果驗證失敗，重新載入下拉選單資料並返回頁面
                await PopulateMailTemplatesDropDownListAsync(_context, Campaign.MailTemplateId);
                await PopulateLandingPageTemplatesDropDownListAsync(_context, Campaign.LandingPageTemplateId);
                await PopulateTargetGroupsDropDownListAsync(_context, Campaign.TargetGroupId); // *** 重新載入 ***
                return Page();
            }

            // **注意：** 這裡只儲存 Campaign 的基本資訊，包括選擇的 TargetGroupId (如果有的話)。
            // 並不直接將群組成員加入 CampaignTargets。這一步會在發送時處理。

            _context.Campaigns.Add(Campaign);
            await _context.SaveChangesAsync();

            // *** 新增：處理 Hangfire 排程 ***
            if (Campaign is { IsAutoSend: true, ScheduledSendTime: not null } && Campaign.ScheduledSendTime.Value > DateTime.UtcNow)
            {
                try
                {
                    var jobId = _backgroundJobClient.Schedule<ICampaignExecutionService>(
                        service => service.ExecuteCampaignAsync(Campaign.Id, _baseUrl, CancellationToken.None),
                        Campaign.ScheduledSendTime.Value);

                    _logger.LogInformation("已為活動 ID: {CampaignId} 排定自動發送工作 JobId: {JobId}...", Campaign.Id, jobId);

                    // *** 修改：儲存 JobId 並更新狀態 ***
                    Campaign.JobId = jobId; // 儲存 JobId
                    Campaign.Status = CampaignStatus.Scheduled; // 更新狀態
                    Campaign.UpdateTime = DateTime.UtcNow;
                    await _context.SaveChangesAsync(); // 再次儲存以更新 JobId 和狀態

                    TempData["SuccessMessage"] = $"演練活動已成功建立，並已排定在 {Campaign.ScheduledSendTime.Value:yyyy-MM-dd HH:mm} 自動發送。";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "排定活動 ID: {CampaignId} 的自動發送工作時發生錯誤。", Campaign.Id);
                    TempData["WarningMessage"] = "活動已建立，但排程自動發送時發生錯誤，請稍後手動啟動或重新編輯排程。";
                    // 狀態保持為 Draft
                }
            }
            else
            {
                TempData["SuccessMessage"] = "演練活動已成功建立！"; // 未排程
            }
            // *** Hangfire 排程處理結束 ***


            return RedirectToPage("./Index");
        }
    }
}