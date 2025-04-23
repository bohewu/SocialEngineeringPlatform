using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SocialEngineeringPlatform.Web.Data;
using SocialEngineeringPlatform.Web.Models.Configuration;
using SocialEngineeringPlatform.Web.Models.Enums;
using SocialEngineeringPlatform.Web.Services.Interfaces; 

namespace SocialEngineeringPlatform.Web.BackgroundServices
{
    public class ScheduledCampaignSender : BackgroundService
    {
        private readonly ILogger<ScheduledCampaignSender> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);
        private readonly string _baseUrl;

        public ScheduledCampaignSender(ILogger<ScheduledCampaignSender> logger,
            IServiceScopeFactory scopeFactory,
            IOptions<AppSettings> appSettings)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _baseUrl = appSettings.Value.BaseUrl; // 確保有 BaseUrl
            if (string.IsNullOrWhiteSpace(appSettings.Value.BaseUrl))
            {
                _logger.LogWarning("AppSettings 中未設定 BaseUrl...");
            }

            _logger.LogInformation("排程發送服務已初始化。檢查間隔：{Interval}, BaseUrl: {BaseUrl}", _checkInterval, _baseUrl);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("排程發送服務正在啟動。");
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("排程發送服務正在執行檢查... ({CurrentTime})", DateTime.Now);
                try
                {
                    // *** 修改：傳入 stoppingToken ***
                    await ProcessScheduledCampaignsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "排程發送服務在執行檢查時發生未預期的錯誤。");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }

                try
                {
                    await Task.Delay(_checkInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            _logger.LogInformation("排程發送服務正在停止。");
        }

        private async Task ProcessScheduledCampaignsAsync(CancellationToken stoppingToken)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                // *** 修改：取得 ICampaignExecutionService ***
                var campaignExecutionService = scope.ServiceProvider.GetRequiredService<ICampaignExecutionService>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<ScheduledCampaignSender>>();

                // *** 修改：查詢時不再需要 Include 關聯資料，因為會在 Service 中處理 ***
                var campaignsToProcess = await context.Campaigns
                    .Where(c => c.Status == CampaignStatus.Scheduled && c.IsAutoSend == true &&
                                c.ScheduledSendTime.HasValue && c.ScheduledSendTime.Value <= DateTime.UtcNow)
                    .Select(c => c.Id) // *** 只選擇 ID ***
                    .ToListAsync(stoppingToken);

                if (!campaignsToProcess.Any())
                {
                    logger.LogInformation("沒有找到需要自動發送的排程活動。");
                    return;
                }

                logger.LogInformation("找到 {Count} 個需要自動發送的排程活動。", campaignsToProcess.Count);


                // *** 移除：AngleSharp 相關程式碼移到 Service 中 ***
                // var angleSharpConfig = Configuration.Default;
                // var browsingContext = BrowsingContext.New(angleSharpConfig);

                foreach (var campaignId in campaignsToProcess) // *** 遍歷 ID ***
                {
                    if (stoppingToken.IsCancellationRequested)
                    {
                        logger.LogWarning("排程發送服務被要求停止...");
                        return;
                    }

                    logger.LogInformation("嘗試執行排程活動 ID: {CampaignId}", campaignId);

                    // *** 重構：呼叫 CampaignExecutionService ***
                    try
                    {
                        var result =
                            await campaignExecutionService.ExecuteCampaignAsync(campaignId, _baseUrl, stoppingToken);
                        if (result.Success)
                        {
                            logger.LogInformation("活動 ID: {CampaignId} 自動執行完成。結果: {ResultMessage}", campaignId,
                                result.Message);
                        }
                        else
                        {
                            logger.LogError("活動 ID: {CampaignId} 自動執行失敗。原因: {ResultMessage}", campaignId,
                                result.Message);
                            // 可以在這裡加入額外的錯誤處理或通知機制
                        }
                    }
                    catch (Exception ex)
                    {
                        // 捕捉 ExecuteCampaignAsync 中未處理的例外
                        logger.LogError(ex, "執行活動 ID: {CampaignId} 時發生未預期的錯誤。", campaignId);
                    }
                    // *** 重構結束 ***

                    // *** 移除：原本的發送循環和 SaveChanges 邏輯已移到 Service 中 ***
                } // End foreach campaignId

                // *** 移除：儲存邏輯已移到 Service 中 ***
                // try { await context.SaveChangesAsync(stoppingToken); ... } catch { ... }
            } // End using scope
        }

        // *** 假設有一個 AppSettings 類別用於讀取 BaseUrl ***
        // --- 需要在 Models/Configuration/AppSettings.cs 中定義 ---
        /*
        namespace SocialEngineeringPlatform.Web.Models.Configuration
        {
            public class AppSettings
            {
                public string? BaseUrl { get; set; }
                // 可以加入 AdminUser 等其他設定
            }
        }
        */
        // --- 需要在 Program.cs 中註冊 AppSettings ---
        /*
        builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
        */
    }
}