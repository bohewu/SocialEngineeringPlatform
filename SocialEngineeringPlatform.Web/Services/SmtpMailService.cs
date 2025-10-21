using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.DataProtection;
using MimeKit;
using SocialEngineeringPlatform.Web.Common;
using SocialEngineeringPlatform.Web.Models.Core;
using SocialEngineeringPlatform.Web.Services.Interfaces;
using IMailService = SocialEngineeringPlatform.Web.Services.Interfaces.IMailService;
using static SocialEngineeringPlatform.Web.Common.LoggingHelper;

namespace SocialEngineeringPlatform.Web.Services
{
    // 使用 MailKit 實作郵件發送服務
    public class SmtpMailService : IMailService
    {
        private readonly ISettingsService _settingsService;
        private readonly ILogger<SmtpMailService> _logger;
        private readonly IDataProtector _dataProtector;

        public SmtpMailService(
            ISettingsService settingsService,
            ILogger<SmtpMailService> logger, IDataProtectionProvider protectionProvider
        )
        {
            _settingsService = settingsService;
            _logger = logger;
            _dataProtector = protectionProvider.CreateProtector(AppConstants.SmtpPasswordProtectionPurpose); // *** 建立 Data Protector ***
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody, string? fromAddress = null, string? fromDisplayName = null)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                _logger.LogWarning("嘗試發送郵件，但收件者 Email 為空。");
                return false;
            }

            // *** 修改：從 ISettingsService 取得設定 ***
            DbSmtpSetting? smtpSettings = await _settingsService.GetSmtpSettingsAsync();

            // 檢查設定是否存在且完整
            if (smtpSettings == null || string.IsNullOrWhiteSpace(smtpSettings.Host) ||
                string.IsNullOrWhiteSpace(smtpSettings.FromAddress))
            {
                _logger.LogError("無法發送郵件，因為從資料庫讀取的 SMTP 設定不完整或不存在。");
                return false;
            }

            // *** 修改：解密密碼 ***
            string? decryptedPassword = null;
            if (!string.IsNullOrEmpty(smtpSettings.EncryptedPassword))
            {
                try
                {
                    decryptedPassword = _dataProtector.Unprotect(smtpSettings.EncryptedPassword);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "無法解密 SMTP 密碼，郵件可能無法驗證。");
                    // 根據策略決定是否繼續嘗試 (如果 SMTP 不需要驗證)
                    // return false; // 如果需要驗證，則直接失敗
                }
            }

            try
            {
                // 建立 MimeMessage 物件 (MailKit 使用 MimeMessage)
                var message = new MimeMessage();
                
                // *** 修改：決定實際使用的 From Address 和 Display Name ***
                string actualFromAddress = !string.IsNullOrWhiteSpace(fromAddress)
                    ? fromAddress
                    : smtpSettings.FromAddress; // Fallback 到系統預設
                string actualFromDisplayName = !string.IsNullOrWhiteSpace(fromDisplayName)
                    ? fromDisplayName
                    : smtpSettings.FromDisplayName ?? ""; // Fallback 到系統預設

                if (string.IsNullOrWhiteSpace(actualFromAddress))
                {
                    _logger.LogError("無法發送郵件，因為最終的寄件者地址為空 (範本未設定且系統預設也為空)。");
                    return false;
                }
                
                // 設定寄件者
                message.From.Add(new MailboxAddress(actualFromDisplayName, actualFromAddress));
                // 設定收件者 (MailKit 可處理多個收件者，此處為單一)
                message.To.Add(MailboxAddress.Parse(toEmail));
                // 設定主旨
                message.Subject = subject;

                // 建立郵件內文
                var builder = new BodyBuilder();
                builder.HtmlBody = htmlBody; // 設定 HTML 內文
                // builder.TextBody = "Plain text version if needed"; // 可選：設定純文字版本
                message.Body = builder.ToMessageBody(); // 將內文設定給 message

                // 建立 MailKit 的 SmtpClient
                using (var client = new SmtpClient())
                {
                    var secureSocketOptions = GetSecureSocketOptions(smtpSettings); // *** 傳入 settings ***
                    _logger.LogDebug("Connecting to SMTP host {Host}:{Port} using {SecureSocketOptions}",
                        smtpSettings.Host, smtpSettings.Port, secureSocketOptions);
                    await client.ConnectAsync(smtpSettings.Host, smtpSettings.Port, secureSocketOptions,
                        CancellationToken.None);
                    _logger.LogDebug("Connected to SMTP host.");

                    // *** 修改：使用解密後的密碼進行驗證 ***
                    if (!string.IsNullOrWhiteSpace(smtpSettings.Username) &&
                        !string.IsNullOrWhiteSpace(decryptedPassword))
                    {
                        _logger.LogDebug("Authenticating SMTP user {Username}", smtpSettings.Username);
                        await client.AuthenticateAsync(smtpSettings.Username, decryptedPassword,
                            CancellationToken.None); // *** 使用解密後的密碼 ***
                        _logger.LogDebug("Authenticated successfully.");
                    }
                    // 如果 Username 或解密後密碼為空，則不進行驗證

                    _logger.LogInformation("正在嘗試發送郵件至 {Recipient}", MaskEmail(toEmail));
                    await client.SendAsync(message, CancellationToken.None);
                    _logger.LogInformation("郵件已成功發送至 {Recipient}", MaskEmail(toEmail));
                    await client.DisconnectAsync(true, CancellationToken.None);
                    _logger.LogDebug("Disconnected from SMTP host.");
                    return true;
                }
            }
            // 捕捉 MailKit 特定的例外
            catch (AuthenticationException authEx)
            {
                _logger.LogError(authEx, "SMTP 驗證失敗 (使用者名稱/密碼錯誤?) for user {Username}. Failed to send to recipient {Recipient}", smtpSettings.Username, MaskEmail(toEmail));
                return false;
            }
            catch (ServiceNotConnectedException sncEx)
            {
                _logger.LogError(sncEx, "無法連線到 SMTP 伺服器 {Host}:{Port}", smtpSettings.Host, smtpSettings.Port);
                return false;
            }
            catch (SmtpCommandException smtpCmdEx) // MailKit 的 SmtpException 在 MailKit.Net.Smtp 命名空間
            {
                _logger.LogError(smtpCmdEx,
                    "發送郵件至 {Recipient} 時發生 SMTP 命令錯誤。StatusCode: {StatusCode}, Mailbox: {Mailbox}", MaskEmail(toEmail),
                    smtpCmdEx.StatusCode, smtpCmdEx.Mailbox?.Address);
                return false;
            }
            catch (Exception ex) // 捕捉其他可能的例外
            {
                _logger.LogError(ex, "發送郵件至 {Recipient} 時發生未預期的錯誤。", MaskEmail(toEmail));
                return false;
            }
        }

        // 輔助方法：根據設定決定 MailKit 的 SecureSocketOptions
        private SecureSocketOptions GetSecureSocketOptions(DbSmtpSetting settings)
        {
            if (settings.Port == 465) return SecureSocketOptions.SslOnConnect;
            if (settings.EnableSsl) return SecureSocketOptions.StartTls;
            return SecureSocketOptions.None;
        }
    }
}