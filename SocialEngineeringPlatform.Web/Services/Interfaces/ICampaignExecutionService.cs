using SocialEngineeringPlatform.Web.ViewModels;

namespace SocialEngineeringPlatform.Web.Services.Interfaces;

public interface ICampaignExecutionService
{
    /// <summary>
    /// 執行指定的演練活動。
    /// </summary>
    /// <param name="campaignId">要執行的活動 ID。</param>
    /// <param name="baseUrl">應用程式的基底 URL (用於產生追蹤連結)。</param>
    /// <param name="cancellationToken">用於取消操作的 Token。</param>
    /// <returns>包含執行結果的 CampaignExecutionResult 物件。</returns>
    Task<CampaignExecutionResult> ExecuteCampaignAsync(int campaignId, string baseUrl, CancellationToken cancellationToken = default);
}