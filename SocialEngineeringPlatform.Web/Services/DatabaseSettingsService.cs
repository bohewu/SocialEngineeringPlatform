using Microsoft.AspNetCore.DataProtection; // *** 加入 DataProtection using ***
using Microsoft.EntityFrameworkCore;
using SocialEngineeringPlatform.Web.Common;
using SocialEngineeringPlatform.Web.Data;
using SocialEngineeringPlatform.Web.Models.Core;
using SocialEngineeringPlatform.Web.Services.Interfaces;

namespace SocialEngineeringPlatform.Web.Services
{
    public class DatabaseSettingsService : ISettingsService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DatabaseSettingsService> _logger;
        private readonly IDataProtector _dataProtector; // *** 注入 IDataProtector ***
        private const int SettingsId = 1; // 假設我們只使用 ID 為 1 的記錄
        public DatabaseSettingsService(
            ApplicationDbContext context,
            ILogger<DatabaseSettingsService> logger,
            IDataProtectionProvider protectionProvider) // *** 注入 IDataProtectionProvider ***
        {
            _context = context;
            _logger = logger;
            // *** 建立特定目的的 Data Protector ***
            _dataProtector = protectionProvider.CreateProtector(AppConstants.SmtpPasswordProtectionPurpose);
        }

        public async Task<DbSmtpSetting?> GetSmtpSettingsAsync()
        {
            try
            {
                // 查找 ID 為 SettingsId 的記錄
                var settings = await _context.SmtpSettings
                                     .AsNoTracking() // 通常讀取設定不需要追蹤
                                     .FirstOrDefaultAsync(s => s.Id == SettingsId);

                if (settings != null && !string.IsNullOrEmpty(settings.EncryptedPassword))
                {
                    // *** 解密密碼 ***
                    try
                    {
                        // 注意：這裡只是為了方便 SmtpMailService 使用，
                        // 不應該將解密後的密碼存回 settings 物件或傳遞到 UI
                        // 更好的做法是 SmtpMailService 也注入 IDataProtector 來解密
                        // 或者提供一個 GetDecryptedPassword 的方法 (但有安全風險)
                        // 暫時先不解密，讓 SmtpMailService 處理
                        // string decryptedPassword = _dataProtector.Unprotect(settings.EncryptedPassword);
                        // _logger.LogDebug("SMTP Password decrypted for temporary use.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "解密 SMTP 密碼時失敗。");
                        // 可以選擇清除 EncryptedPassword 或返回 null/錯誤
                        settings.EncryptedPassword = null; // 清除無法解密的密碼
                    }
                }
                else if (settings == null)
                {
                     _logger.LogWarning("資料庫中找不到 SMTP 設定 (ID={SettingsId})。", SettingsId);
                     // 可以考慮返回一個包含預設值的空物件，或讓呼叫端處理 null
                     // return new DbSmtpSetting { Id = SettingsId, Port = 587, EnableSsl = true, FromAddress="default@example.com" };
                }

                return settings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "讀取 SMTP 設定時發生錯誤。");
                return null;
            }
        }

        public async Task<bool> UpdateSmtpSettingsAsync(DbSmtpSetting settings, string? plainPassword = null)
        {
            if (settings is not { Id: SettingsId })
            {
                _logger.LogWarning("嘗試更新無效的 SMTP 設定。傳入的 ID: {InputId}", settings?.Id);
                return false;
            }

            try
            {
                var existingSettings = await _context.SmtpSettings.FindAsync(SettingsId);
                if (existingSettings == null)
                {
                    // 理論上不應發生，因為 HasData 會建立記錄
                    _logger.LogError("嚴重錯誤：找不到 ID={SettingsId} 的 SMTP 設定記錄，即使它應該由 HasData 建立。", SettingsId);
                    return false; // 或者拋出例外
                }

                // 更新欄位值
                existingSettings.Host = settings.Host;
                existingSettings.Port = settings.Port;
                existingSettings.EnableSsl = settings.EnableSsl;
                existingSettings.Username = settings.Username;
                existingSettings.FromAddress = settings.FromAddress;
                existingSettings.FromDisplayName = settings.FromDisplayName;

                // 如果提供了新的明文密碼，則加密並更新
                if (!string.IsNullOrWhiteSpace(plainPassword))
                {
                    try
                    {
                        existingSettings.EncryptedPassword = _dataProtector.Protect(plainPassword);
                        _logger.LogInformation("SMTP 密碼已加密並準備更新。");
                    }
                    catch (Exception ex)
                    {
                         _logger.LogError(ex, "加密新 SMTP 密碼時失敗。");
                         // 根據策略決定是否繼續儲存其他設定
                         return false; // 加密失敗，不儲存
                    }
                }
                // 如果沒提供新密碼，則保持現有的 EncryptedPassword 不變

                _context.Entry(existingSettings).State = EntityState.Modified; // 標記為已修改
                await _context.SaveChangesAsync();
                _logger.LogInformation("SMTP 設定已成功更新。");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新 SMTP 設定時發生錯誤。");
                return false;
            }
        }
    }
}