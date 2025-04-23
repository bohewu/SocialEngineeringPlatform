using System.Text;
using AngleSharp;
using AngleSharp.Html.Dom;
using Microsoft.EntityFrameworkCore;
using SocialEngineeringPlatform.Web.Common;
using SocialEngineeringPlatform.Web.Data;
using SocialEngineeringPlatform.Web.Models.Core;
using SocialEngineeringPlatform.Web.Models.Enums;
using SocialEngineeringPlatform.Web.Services.Interfaces;
using SocialEngineeringPlatform.Web.ViewModels; // For CampaignExecutionResult

namespace SocialEngineeringPlatform.Web.Services
{
    public class CampaignExecutionService : ICampaignExecutionService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMailService _mailService;
        private readonly ILogger<CampaignExecutionService> _logger;
        private readonly ISettingsService _settingsService; // *** 注入設定服務以獲取預設寄件者 ***
        public CampaignExecutionService(
            ApplicationDbContext context,
            IMailService mailService,
            ILogger<CampaignExecutionService> logger, ISettingsService settingsService)
        {
            _context = context;
            _mailService = mailService;
            _logger = logger;
            _settingsService = settingsService;
        }

        public async Task<CampaignExecutionResult> ExecuteCampaignAsync(int campaignId, string baseUrl,
            CancellationToken cancellationToken = default)
        {
            var result = new CampaignExecutionResult();

            // --- 1. 查詢並驗證活動 ---
            var campaign = await _context.Campaigns
                .Include(c => c.MailTemplate)
                .Include(c => c.CampaignTargets) // 仍然需要載入以處理手動模式或清理
                .ThenInclude(ct => ct.TargetUser)
                .FirstOrDefaultAsync(c => c.Id == campaignId, cancellationToken);

            if (campaign == null)
            {
                /* ... 錯誤處理 ... */
                result.Message = $"找不到 ID 為 {campaignId} 的活動。";
                _logger.LogWarning(result.Message);
                return result;
            }

            if (campaign.Status != CampaignStatus.Draft && campaign.Status != CampaignStatus.Scheduled)
            {
                result.Message = $"活動 ID: {campaignId} 的狀態為 {campaign.Status}，無法啟動（必須是 Draft 或 Scheduled）。";
                _logger.LogWarning(result.Message);
                return result;
            }

            if (campaign.MailTemplate == null)
            {
                /* ... 錯誤處理 ... */
                result.Message = $"活動 ID: {campaignId} 未設定郵件範本，無法啟動。";
                _logger.LogWarning(result.Message);
                return result;
            }
            // --- 驗證結束 ---

            // *** 獲取預設 SMTP 設定 (用於 fallback) ***
            var defaultSmtpSettings = await _settingsService.GetSmtpSettingsAsync();
            if (defaultSmtpSettings == null || string.IsNullOrWhiteSpace(defaultSmtpSettings.FromAddress))
            {
                result.Message = $"無法執行活動 ID: {campaignId}，因為系統預設 SMTP 設定不完整。";
                _logger.LogError(result.Message);
                return result;
            }
            

            // --- 2. 準備最終目標列表並同步 CampaignTargets (簡化邏輯) ---
            List<CampaignTarget> targetsToSend; // 最終要發送的目標列表

            if (campaign.TargetGroupId.HasValue) // 如果指定了群組
            {
                _logger.LogInformation("活動 ID: {CampaignId} 指定了目標群組 ID: {GroupId}，目標將以此群組成員為主。", campaignId,
                    campaign.TargetGroupId.Value);

                // 找出群組內所有啟用成員的 ID
                var groupMemberIds = await _context.TargetUsers
                    .Where(u => u.GroupId == campaign.TargetGroupId.Value && u.IsActive)
                    .Select(u => u.Id)
                    .ToListAsync(cancellationToken);

                // *** 清理：移除所有現有的 CampaignTargets (因為目標由群組決定) ***
                if (campaign.CampaignTargets.Any())
                {
                    _logger.LogInformation("移除活動 ID: {CampaignId} 的現有手動目標，因為已指定群組。", campaignId);
                    // 不能直接 RemoveRange campaign.CampaignTargets，因為 EF Core 會混淆
                    var existingTargets = campaign.CampaignTargets.ToList(); // 複製一份
                    _context.CampaignTargets.RemoveRange(existingTargets); // 從 DbContext 移除
                    // 不需要從 campaign.CampaignTargets 集合移除，因為後面會重新賦值
                }

                // *** 新增：為群組成員建立新的 CampaignTarget 記錄 ***
                targetsToSend = new List<CampaignTarget>(); // 建立新的列表
                if (groupMemberIds.Any())
                {
                    _logger.LogInformation("為活動 ID: {CampaignId} 建立 {Count} 個來自群組的目標記錄。", campaignId,
                        groupMemberIds.Count);
                    foreach (var userIdToAdd in groupMemberIds)
                    {
                        var newTarget = new CampaignTarget
                        {
                            CampaignId = campaignId, // 明確設定 CampaignId
                            TargetUserId = userIdToAdd,
                            SendStatus = CampaignSendStatus.Pending
                        };
                        targetsToSend.Add(newTarget);
                        // *** 注意：這裡不直接 Add 到 campaign.CampaignTargets，因為我們在下面會 AddRange ***
                    }

                    // 將新建立的目標加入 DbContext
                    _context.CampaignTargets.AddRange(targetsToSend);
                }
            }
            else // 未指定群組，使用手動管理的目標
            {
                _logger.LogInformation("活動 ID: {CampaignId} 未指定目標群組，使用手動管理的目標列表。", campaignId);
                // 直接使用已載入的 CampaignTargets
                targetsToSend = campaign.CampaignTargets.ToList();
                // 確保手動加入的目標使用者資料已載入 (雖然 Include 應該已處理)
                foreach (var ct in targetsToSend)
                {
                    if (ct.TargetUser == null)
                    {
                        ct.TargetUser = await _context.TargetUsers.FindAsync(ct.TargetUserId);
                    }
                }
            }

            // 再次檢查是否有目標
            if (!targetsToSend.Any())
            {
                result.Message = $"活動 ID: {campaignId} 沒有可發送的目標對象。";
                _logger.LogWarning(result.Message);
                // 可以考慮將活動狀態設為錯誤或取消
                // campaign.Status = CampaignStatus.Cancelled;
                // await _context.SaveChangesAsync();
                return result;
            }
            // --- 目標列表準備完成 ---


            // --- 3. 更新活動狀態並準備發送 ---
            _logger.LogInformation("準備發送活動 ID: {CampaignId} 給 {TargetCount} 個目標。", campaignId, targetsToSend.Count);
            if (campaign.Status == CampaignStatus.Draft)
            {
                campaign.ActualStartTime = DateTime.UtcNow;
            }
            campaign.Status = CampaignStatus.Running; // 無論如何都設為 Running
            campaign.UpdateTime = DateTime.UtcNow;

            var angleSharpConfig = Configuration.Default;
            var browsingContext = BrowsingContext.New(angleSharpConfig);

            // --- 4. 循環發送郵件 ---
            foreach (var campaignTarget in targetsToSend) // *** 使用新的 targetsToSend 列表 ***
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    /* ... 取消處理 ... */
                    result.Message = $"活動 ID: {campaignId} 的發送過程被取消。";
                    _logger.LogWarning(result.Message);
                    return result;
                }

                // TargetUser 應該已在準備列表時載入
                if (campaignTarget.TargetUser == null || string.IsNullOrWhiteSpace(campaignTarget.TargetUser.Email))
                {
                    _logger.LogWarning("目標 TargetUserId: {Tid} 缺少 Email 或使用者資料，跳過發送。", campaignTarget.TargetUserId);
                    result.FailCount++;
                    campaignTarget.SendStatus = CampaignSendStatus.Failed; // 更新狀態
                    campaignTarget.ErrorMessage = "目標使用者 Email 為空或資料缺失";
                    continue;
                }

                // 檢查是否已發送過 (避免重試時重複發送)
                if (campaignTarget.SendStatus == CampaignSendStatus.Sent ||
                    campaignTarget.SendStatus == CampaignSendStatus.Failed)
                {
                    _logger.LogInformation("目標 TargetUserId: {Tid} 的狀態為 {Status}，跳過重複發送。", campaignTarget.TargetUserId,
                        campaignTarget.SendStatus);
                    if (campaignTarget.SendStatus == CampaignSendStatus.Sent) result.SuccessCount++;
                    else result.FailCount++;
                    continue;
                }


                string toEmail = campaignTarget.TargetUser.Email;
                string subject = campaign.MailTemplate!.Subject;
                string rawHtmlBody = campaign.MailTemplate.Body;
                string modifiedHtmlBody = rawHtmlBody;
                
                // *** 修改：決定寄件者資訊 ***
                string fromAddress = defaultSmtpSettings.FromAddress; // 預設使用系統設定
                string? fromDisplayName = defaultSmtpSettings.FromDisplayName;

                
                if (!string.IsNullOrWhiteSpace(campaign.MailTemplate.CustomFromAddress))
                {
                    fromAddress = campaign.MailTemplate.CustomFromAddress;
                    // 如果自訂了地址但沒自訂名稱，可以選擇使用自訂地址或系統預設名稱
                    fromDisplayName = !string.IsNullOrWhiteSpace(campaign.MailTemplate.CustomFromDisplayName)
                        ? campaign.MailTemplate.CustomFromDisplayName
                        : fromDisplayName; // 或者設為 null/空字串
                    _logger.LogDebug("活動 ID: {Cid}, 目標: {Tid} 使用範本自訂寄件者: {From}", campaignId, campaignTarget.TargetUserId, fromAddress);
                }
                else if (!string.IsNullOrWhiteSpace(campaign.MailTemplate.CustomFromDisplayName))
                {
                    // 如果只自訂了名稱，地址仍然用預設的
                    fromDisplayName = campaign.MailTemplate.CustomFromDisplayName;
                    _logger.LogDebug("活動 ID: {Cid}, 目標: {Tid} 使用範本自訂寄件者名稱: {FromName}", campaignId, campaignTarget.TargetUserId, fromDisplayName);
                }
                // *** 寄件者資訊決定完畢 ***
                

                try // --- 處理連結替換和追蹤像素 ---
                {
                    var document = await browsingContext.OpenAsync(req => req.Content(rawHtmlBody), cancellationToken);
                    var links = document.QuerySelectorAll("a[href]");
                    foreach (var link in links.OfType<IHtmlAnchorElement>())
                    {
                        string originalHref = link.GetAttribute("href") ?? ""; // 取得原始 href

                        // *** 修改：判斷連結類型並產生對應 URL ***
                        string finalUrl;
                        if (originalHref.Equals(AppConstants.PhishingLinkPlaceholder,
                                StringComparison.OrdinalIgnoreCase) && campaign.LandingPageTemplateId.HasValue)
                        {
                            // 這是釣魚連結，且活動設定了登陸頁 -> 產生登陸頁追蹤連結
                            finalUrl =
                                $"{baseUrl}/Track/Landing?c={campaign.Id}&t={campaignTarget.TargetUserId}"; // 指向新的端點
                            _logger.LogTrace("釣魚連結替換 -> '{Tracking}'", finalUrl);
                        }
                        else if (!string.IsNullOrWhiteSpace(originalHref) &&
                                 Uri.TryCreate(originalHref, UriKind.Absolute, out var uri) &&
                                 (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                        {
                            // 這是普通外部連結 -> 產生點擊追蹤連結
                            byte[] urlBytes = Encoding.UTF8.GetBytes(originalHref);
                            string encodedUrl = Convert.ToBase64String(urlBytes).Replace('+', '-').Replace('/', '_')
                                .TrimEnd('=');
                            finalUrl =
                                $"{baseUrl}/Track/Click?c={campaign.Id}&t={campaignTarget.TargetUserId}&url={encodedUrl}"; // 指向點擊追蹤端點
                            _logger.LogTrace("普通連結替換: '{Original}' -> '{Tracking}'", originalHref, finalUrl);
                        }
                        else
                        {
                            // 其他連結 (mailto:, #fragment, javascript:, 相對路徑等) 或無效連結 -> 保持原樣
                            finalUrl = originalHref;
                            _logger.LogTrace("跳過連結 (無效或不需追蹤): '{Original}'", originalHref);
                        }

                        link.SetAttribute("href", finalUrl); // 設定最終的 href
                        // *** 連結處理結束 ***
                    }

                    if (campaign.TrackOpens)
                    {
                        /* ... 附加像素，使用 baseUrl ... */
                        string pixelUrl = $"{baseUrl}/Track/Open?c={campaign.Id}&t={campaignTarget.TargetUserId}";
                        var bodyElement = document.Body;
                        if (bodyElement != null)
                        {
                            var pixelElement = document.CreateElement("img");
                            if (pixelElement is IHtmlImageElement pixelImg)
                            {
                                pixelImg.Source = pixelUrl;
                                pixelImg.AlternativeText = "";
                                pixelImg.SetAttribute("style", "display:none;border:0;width:1px;height:1px;");
                                bodyElement.AppendChild(pixelImg);
                            }
                        }
                    }

                    modifiedHtmlBody = document.ToHtml();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "處理郵件內文時發生錯誤。CampaignId={Cid}, TargetUserId={Tid}", campaign.Id,
                        campaignTarget.TargetUserId);
                }
                // --- 處理結束 ---
                
                // bool sentSuccessfully = await _mailService.SendEmailAsync(toEmail, subject, modifiedHtmlBody); // 原本的呼叫
                bool sentSuccessfully = await _mailService.SendEmailAsync(toEmail, subject, modifiedHtmlBody, fromAddress, fromDisplayName); // 修改後的呼叫

                var log = new MailSendLog
                {
                    CampaignId = campaignId, // 設定 FK
                    TargetUserId = campaignTarget.TargetUserId,
                    SendTime = DateTime.UtcNow,
                    Status = sentSuccessfully ? CampaignSendStatus.Sent : CampaignSendStatus.Failed,
                    ErrorMessage = sentSuccessfully ? null : "郵件服務發送失敗"
                };
                campaign.MailSendLogs.Add(log); // 使用導覽屬性加入 Log

                // 更新 CampaignTarget 狀態
                campaignTarget.SendStatus = log.Status;
                campaignTarget.ErrorMessage = log.ErrorMessage;
                campaignTarget.SendTime = log.SendTime;
                // EF Core 會自動追蹤 campaignTarget 的變更

                if (sentSuccessfully) result.SuccessCount++;
                else result.FailCount++;
                if (campaign.SendBatchDelaySeconds > 0)
                    await Task.Delay(TimeSpan.FromSeconds(campaign.SendBatchDelaySeconds), cancellationToken);
            } // End foreach targetUserId

            // --- 5. 儲存所有變更 ---
            try
            {
                // SaveChanges 會儲存 Campaign 更新、新增/移除/更新的 CampaignTargets、以及新增的 MailSendLogs
                await _context.SaveChangesAsync(cancellationToken);
                result.Success = true;
                result.Message = $"活動 '{campaign.Name}' 發送處理完成。成功: {result.SuccessCount}, 失敗: {result.FailCount}";
                _logger.LogInformation(result.Message);
            }
            catch (DbUpdateConcurrencyException dbEx)
            {
                /* ... 錯誤處理 ... */
                result.Message = "儲存發送結果時發生併發衝突。";
                _logger.LogError(dbEx, result.Message + " CampaignId={Cid}", campaignId);
            }
            catch (Exception ex)
            {
                /* ... 錯誤處理 ... */
                result.Message = $"儲存發送結果時發生錯誤：{ex.Message}";
                _logger.LogError(ex, result.Message + " CampaignId={Cid}", campaignId);
            }

            return result;
        }
    }
}