namespace SocialEngineeringPlatform.Web.Services.Interfaces;

public interface IMailService
{
    /// <summary>
    /// 非同步發送電子郵件。
    /// </summary>
    /// <param name="toEmail">收件者 Email 地址。</param>
    /// <param name="subject">郵件主旨。</param>
    /// <param name="htmlBody">郵件內容 (HTML 格式)。</param>
    /// <param name="fromAddress">寄件者 Email 地址 (如果為 null 或空，則使用系統預設值)。</param>
    /// <param name="fromDisplayName">寄件者顯示名稱 (如果為 null 或空，則使用系統預設值)。</param>
    /// <returns>發送成功返回 true，失敗返回 false。</returns>
    Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody, string? fromAddress = null, string? fromDisplayName = null); // *** 加入 fromAddress 和 fromDisplayName ***
}