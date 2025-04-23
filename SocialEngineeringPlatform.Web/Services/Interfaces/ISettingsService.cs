using SocialEngineeringPlatform.Web.Models.Core;

namespace SocialEngineeringPlatform.Web.Services.Interfaces;

public interface ISettingsService
{
    /// <summary>
    /// 非同步取得目前的 SMTP 設定。
    /// </summary>
    /// <returns>SMTP 設定物件，如果未設定則可能為 null 或包含預設值。</returns>
    Task<DbSmtpSetting?> GetSmtpSettingsAsync();

    /// <summary>
    /// 非同步更新 SMTP 設定。
    /// </summary>
    /// <param name="settings">包含新設定的物件。</param>
    /// <param name="plainPassword">未加密的密碼 (如果需要更新)。</param>
    /// <returns>表示操作是否成功的布林值。</returns>
    Task<bool> UpdateSmtpSettingsAsync(DbSmtpSetting settings, string? plainPassword = null);
}