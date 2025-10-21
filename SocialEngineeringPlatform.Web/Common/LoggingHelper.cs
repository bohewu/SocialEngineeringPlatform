namespace SocialEngineeringPlatform.Web.Common;

/// <summary>
/// 提供日誌相關的輔助方法，特別是用於保護隱私資訊
/// </summary>
public static class LoggingHelper
{
    /// <summary>
    /// 遮蔽電子郵件地址以避免在日誌中暴露私人資訊 (PII)
    /// </summary>
    /// <param name="email">要遮蔽的電子郵件地址</param>
    /// <returns>遮蔽後的電子郵件地址，例如: jo***@example.com</returns>
    /// <remarks>
    /// 此方法會保留電子郵件的前2個字元和完整的網域部分，以便於除錯，
    /// 同時遵守 GDPR 和隱私保護最佳實踐。
    /// </remarks>
    public static string MaskEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return "[empty]";

        var atIndex = email.IndexOf('@');
        if (atIndex <= 0)
            return "[invalid]";

        // 保留前2個字元和@之後的部分，中間用***替代
        var localPart = email.Substring(0, atIndex);
        var domainPart = email.Substring(atIndex);
        
        if (localPart.Length <= 2)
            return $"{localPart[0]}***{domainPart}";
        
        return $"{localPart.Substring(0, 2)}***{domainPart}";
    }
}
