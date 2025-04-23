using System.Globalization;
using System.Text;
using CsvHelper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SocialEngineeringPlatform.Web.Data;
using SocialEngineeringPlatform.Web.Models.Enums;
using SocialEngineeringPlatform.Web.ViewModels; // *** 引用 ViewModel ***

namespace SocialEngineeringPlatform.Web.Pages.Campaigns
{
    [Authorize] // 根據需要啟用授權
    public class ResultsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ResultsModel> _logger;

        public ResultsModel(ApplicationDbContext context, ILogger<ResultsModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        // 綁定 ViewModel 到頁面
        public CampaignResultsViewModel Results { get; set; } = new CampaignResultsViewModel();

        // 從路由獲取活動 ID
        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                // 1. 查詢活動基本資訊
                var campaign = await _context.Campaigns
                                     .Include(c => c.MailTemplate) // 包含關聯資料以便顯示
                                     .Include(c => c.LandingPageTemplate)
                                     .AsNoTracking()
                                     .FirstOrDefaultAsync(c => c.Id == Id);

                if (campaign == null)
                {
                    return NotFound($"找不到 ID 為 {Id} 的活動。");
                }
                Results.Campaign = campaign;

                // 2. 查詢目標總數
                Results.TotalTargets = await _context.CampaignTargets
                                             .CountAsync(ct => ct.CampaignId == Id);

                // 3. 查詢發送狀態統計 (從 CampaignTargets 讀取最終狀態)
                var targetSendStatuses = await _context.CampaignTargets
                                               .Where(ct => ct.CampaignId == Id)
                                               .Select(ct => new { ct.TargetUserId, ct.SendStatus, ct.SendTime })
                                               .ToListAsync();

                Results.EmailsSent = targetSendStatuses.Count(t => t.SendStatus == CampaignSendStatus.Sent);
                Results.EmailsFailed = targetSendStatuses.Count(t => t.SendStatus == CampaignSendStatus.Failed);
                // 注意：這裡假設 CampaignTargets.SendStatus 會被正確更新

                // 4. 查詢追蹤事件統計 (只計算每個目標的第一次事件)
                var trackingEvents = await _context.TrackingEvents
                                           .Where(te => te.CampaignId == Id)
                                           .OrderBy(te => te.EventTime) // 確保取到第一次事件
                                           .AsNoTracking()
                                           .ToListAsync();

                // 按事件類型分組，並計算唯一 TargetUserId 的數量
                Results.OpensRecorded = trackingEvents.Where(e => e.EventType == TrackingEventType.Opened).DistinctBy(e => e.TargetUserId).Count();
                Results.ClicksRecorded = trackingEvents.Where(e => e.EventType == TrackingEventType.Clicked).DistinctBy(e => e.TargetUserId).Count();
                Results.SubmissionsRecorded = trackingEvents.Where(e => e.EventType == TrackingEventType.SubmittedData).DistinctBy(e => e.TargetUserId).Count();
                Results.ReportsRecorded = trackingEvents.Where(e => e.EventType == TrackingEventType.ReportedPhish).DistinctBy(e => e.TargetUserId).Count();

                // 5. 計算比率 (以成功發送數為分母，注意除零錯誤)
                if (Results.EmailsSent > 0)
                {
                    // 開啟率 = (唯一開啟使用者數 / 成功發送數)
                    Results.OpenRate = (double)Results.OpensRecorded / Results.EmailsSent;
                    // 點擊率 = (唯一點擊使用者數 / 成功發送數)
                    Results.ClickRate = (double)Results.ClicksRecorded / Results.EmailsSent;
                    // 提交率 = (唯一提交使用者數 / 成功發送數)
                    Results.SubmissionRate = (double)Results.SubmissionsRecorded / Results.EmailsSent;
                }

                // 6. 準備詳細目標互動列表
                var campaignTargets = await _context.CampaignTargets
                                            .Include(ct => ct.TargetUser) // 載入使用者資訊
                                            .Where(ct => ct.CampaignId == Id)
                                            .AsNoTracking()
                                            .ToListAsync();

                // 將追蹤事件按 TargetUserId 分組，並只保留每種事件類型的首次發生
                var firstEventsByTarget = trackingEvents
                                            .GroupBy(te => te.TargetUserId)
                                            .ToDictionary(
                                                g => g.Key,
                                                g => g.GroupBy(e => e.EventType)
                                                      .Select(eg => eg.First()) // 取同類型事件的第一筆
                                                      .ToDictionary(e => e.EventType, e => e.EventTime) // 記錄事件類型和時間
                                            );

                foreach (var ct in campaignTargets)
                {
                    if (ct.TargetUser == null) continue; // 跳過沒有使用者資料的目標

                    var interaction = new TargetInteractionViewModel
                    {
                        TargetUserId = ct.TargetUserId,
                        Email = ct.TargetUser.Email,
                        Name = ct.TargetUser.Name,
                        SendStatus = ct.SendStatus, // 直接從 CampaignTarget 讀取
                        SentTime = ct.SendTime     // 直接從 CampaignTarget 讀取
                    };

                    // 檢查該使用者是否有各種追蹤事件
                    if (firstEventsByTarget.TryGetValue(ct.TargetUserId, out var userEvents))
                    {
                        interaction.Opened = userEvents.ContainsKey(TrackingEventType.Opened);
                        interaction.Clicked = userEvents.ContainsKey(TrackingEventType.Clicked);
                        interaction.Submitted = userEvents.ContainsKey(TrackingEventType.SubmittedData);
                        interaction.Reported = userEvents.ContainsKey(TrackingEventType.ReportedPhish);
                        // 可以選擇性地將 userEvents 中的時間賦值給 ViewModel 的其他屬性
                    }

                    Results.TargetInteractions.Add(interaction);
                }
                // 依照 Email 排序結果列表
                Results.TargetInteractions = Results.TargetInteractions.OrderBy(t => t.Email).ToList();

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "載入活動報告 ID: {CampaignId} 時發生錯誤。", Id);
                TempData["ErrorMessage"] = "載入活動報告時發生錯誤。";
                // return RedirectToPage("/Error");
            }

            return Page();
        }
        
        // *** 新增：處理匯出 CSV 的 Handler ***
        public async Task<IActionResult> OnGetExportCsvAsync()
        {
            try
            {
                // 1. 取得活動名稱 (用於檔名)
                var campaignName = await _context.Campaigns
                    .Where(c => c.Id == Id)
                    .Select(c => c.Name)
                    .FirstOrDefaultAsync() ?? "UnknownCampaign";

                // 2. 取得要匯出的數據 (與 OnGetAsync 類似)
                var dataToExport = await GetTargetInteractionDataAsync(Id);

                // 3. 使用 CsvHelper 產生 CSV 內容
                using (var memoryStream = new MemoryStream())
                    // 使用 StreamWriter 並指定 UTF-8 with BOM 編碼
                using (var writer = new StreamWriter(memoryStream, new UTF8Encoding(true))) // UTF-8 with BOM
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    // 寫入記錄 (CsvHelper 會自動寫入標頭，基於 ViewModel 的 [Name] Attribute)
                    await csv.WriteRecordsAsync(dataToExport);
                    await writer.FlushAsync(); // 確保所有內容寫入 stream

                    // 4. 返回檔案結果
                    var fileName = $"CampaignReport_{campaignName.Replace(" ", "_")}_{DateTime.Now:yyyyMMddHHmm}.csv";
                    return File(memoryStream.ToArray(), "text/csv", fileName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "匯出活動報告 ID: {CampaignId} 時發生錯誤。", Id);
                TempData["ErrorMessage"] = "匯出報告時發生錯誤。";
                return RedirectToPage(new { id = Id }); // 返回報告頁面顯示錯誤
            }
        }
        
         // *** 新增：提取獲取目標互動數據的共用方法 ***
        private async Task<List<TargetInteractionViewModel>> GetTargetInteractionDataAsync(int campaignId)
        {
            var campaignTargets = await _context.CampaignTargets
                                        .Include(ct => ct.TargetUser)
                                        .Where(ct => ct.CampaignId == campaignId)
                                        .AsNoTracking()
                                        .ToListAsync();

            var trackingEvents = await _context.TrackingEvents
                                       .Where(te => te.CampaignId == campaignId)
                                       .OrderBy(te => te.EventTime)
                                       .AsNoTracking()
                                       .ToListAsync();

            var firstEventsByTarget = trackingEvents
                                        .GroupBy(te => te.TargetUserId)
                                        .ToDictionary(
                                            g => g.Key,
                                            g => g.GroupBy(e => e.EventType)
                                                  .Select(eg => eg.First())
                                                  .ToDictionary(e => e.EventType, e => e.EventTime)
                                        );

            var interactions = new List<TargetInteractionViewModel>();
            foreach (var ct in campaignTargets)
            {
                if (ct.TargetUser == null) continue;
                var interaction = new TargetInteractionViewModel
                {
                    TargetUserId = ct.TargetUserId,
                    Email = ct.TargetUser.Email,
                    Name = ct.TargetUser.Name,
                    SendStatus = ct.SendStatus,
                    SentTime = ct.SendTime
                };
                if (firstEventsByTarget.TryGetValue(ct.TargetUserId, out var userEvents))
                {
                    interaction.Opened = userEvents.ContainsKey(TrackingEventType.Opened);
                    interaction.Clicked = userEvents.ContainsKey(TrackingEventType.Clicked);
                    interaction.Submitted = userEvents.ContainsKey(TrackingEventType.SubmittedData);
                    interaction.Reported = userEvents.ContainsKey(TrackingEventType.ReportedPhish);
                }
                interactions.Add(interaction);
            }
            return interactions.OrderBy(t => t.Email).ToList();
        }
    }
    
    
}
