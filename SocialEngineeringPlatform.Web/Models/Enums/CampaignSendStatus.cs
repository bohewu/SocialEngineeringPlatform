namespace SocialEngineeringPlatform.Web.Models.Enums
{
    public enum CampaignSendStatus
    {
        Pending,    // 待發送
        Sent,       // 已發送
        Failed,     // 發送失敗
        Bounced     // 郵件被退回 (可選，需額外處理退信)
    }
}