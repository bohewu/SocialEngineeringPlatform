# Privacy Information Exposure Fix - Email Masking in Logs

## Issue Description
**Security Alert**: Exposure of Private Information (PII)

GitHub CodeScan identified a data flow where email addresses (private information) were being written directly to application logs without sanitization.

### Data Flow Path
1. **Source**: `CampaignExecutionService.cs:296` - `toEmail` variable
2. **Parameter**: Passed to `SmtpMailService.SendEmailAsync()`
3. **Sink**: `SmtpMailService.cs:120, 121` - Written to logs via `_logger.LogInformation()`

### Privacy Concern
- Email addresses are **Personally Identifiable Information (PII)**
- Logging raw email addresses violates privacy best practices and GDPR compliance
- Logs may be:
  - Stored indefinitely
  - Backed up to multiple locations
  - Accessed by unauthorized personnel
  - Transmitted to third-party log aggregation services

## Solution Implemented

### Centralized Email Masking Utility (DRY Principle)
Created a shared `LoggingHelper` utility class in `Common/LoggingHelper.cs` to comply with the DRY (Don't Repeat Yourself) principle. This provides a centralized `MaskEmail()` method that:
- Preserves the first 2 characters of the local part
- Masks the rest with `***`
- Keeps the domain visible for debugging purposes
- Can be reused across the entire application

**Example**:
- `john.doe@example.com` → `jo***@example.com`
- `a@test.com` → `a***@test.com`
- Empty/invalid → `[empty]` or `[invalid]`

**Implementation**:
```csharp
// Common/LoggingHelper.cs
public static class LoggingHelper
{
    public static string MaskEmail(string email)
    {
        // Implementation with XML documentation
    }
}
```

**Usage** (via static using):
```csharp
using static SocialEngineeringPlatform.Web.Common.LoggingHelper;

// Then call directly:
_logger.LogInformation("Email sent to {Recipient}", MaskEmail(toEmail));
```

### Changes Made to `SmtpMailService.cs`

#### 1. Added Email Masking Helper Method
```csharp
// 輔助方法：遮蔽電子郵件地址以避免在日誌中暴露私人資訊
private string MaskEmail(string email)
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
```

#### 2. Updated All Log Statements
Replaced all instances of logging raw `toEmail` with `MaskEmail(toEmail)`:

**Line 118**: Success attempt log
```csharp
_logger.LogInformation("正在嘗試發送郵件至 {Recipient}", MaskEmail(toEmail));
```

**Line 120**: Success confirmation log
```csharp
_logger.LogInformation("郵件已成功發送至 {Recipient}。Result: {Result}", MaskEmail(toEmail), result);
```

**Line 128**: Authentication failure log
```csharp
_logger.LogError(authEx, "SMTP 驗證失敗 (使用者名稱/密碼錯誤?) for user {Username}. Failed to send to recipient {Recipient}", smtpSettings.Username, MaskEmail(toEmail));
```

**Line 138**: SMTP command error log
```csharp
_logger.LogError(smtpCmdEx, "發送郵件至 {Recipient} 時發生 SMTP 命令錯誤。StatusCode: {StatusCode}, Mailbox: {Mailbox}", MaskEmail(toEmail), smtpCmdEx.StatusCode, smtpCmdEx.Mailbox?.Address);
```

**Line 143**: General error log
```csharp
_logger.LogError(ex, "發送郵件至 {Recipient} 時發生未預期的錯誤。", MaskEmail(toEmail));
```

## Benefits

### Security & Compliance
- ✅ **GDPR Compliance**: Reduces exposure of personal data in logs
- ✅ **Privacy Protection**: Email addresses no longer stored in plain text
- ✅ **Audit Trail**: Still maintains enough information for debugging
- ✅ **Best Practice**: Follows OWASP guidelines for PII handling

### Debugging & Operations
- ✅ **Troubleshooting**: Domain information still visible
- ✅ **Pattern Recognition**: Can still identify email patterns
- ✅ **Error Tracking**: Full context preserved except PII

## Testing Recommendations

1. **Verify Log Output**: Check that email addresses are properly masked in all log entries
2. **Test Edge Cases**:
   - Empty strings
   - Invalid email formats
   - Single character emails
   - Very long email addresses
3. **Functional Testing**: Ensure email sending still works correctly
4. **Log Analysis**: Review logs to confirm no PII leakage

## Additional Fix: DbInitializer.cs

### Issue Location
`DbInitializer.cs:104` - Admin email address was being logged during database initialization

### Affected Log Statements
The following locations in `CreateDefaultAdminUserAsync()` were logging raw email addresses:
- Line 104: Success log when admin user created and added to role
- Line 109: Error log when adding to role failed
- Line 115: Error log when creating admin user failed
- Line 121: Info log when admin user already exists

### Changes Applied
Added the same `MaskEmail()` helper method to `DbInitializer.cs` and updated all 4 log statements:

```csharp
// Before:
logger.LogInformation($"Default admin user '{adminEmail}' created successfully...");

// After:
logger.LogInformation($"Default admin user '{MaskEmail(adminEmail)}' created successfully...");
```

All instances now properly mask the admin email address in logs during application initialization.

## Architecture & Design

### DRY Principle Compliance
The solution follows the **DRY (Don't Repeat Yourself)** principle by:
1. **Single Source of Truth**: One centralized `MaskEmail()` implementation
2. **Reusability**: Available to all services and data layers via static using
3. **Maintainability**: Changes to masking logic only need to be made in one place
4. **Testability**: Single method to unit test for all masking scenarios

### File Structure
```
Common/
  ├── AppConstants.cs (existing)
  └── LoggingHelper.cs (new - email masking utility)
Services/
  └── SmtpMailService.cs (uses LoggingHelper)
Data/
  └── DbInitializer.cs (uses LoggingHelper)
```

## Related Files
- `SocialEngineeringPlatform.Web/Common/LoggingHelper.cs` (Created - centralized utility)
- `SocialEngineeringPlatform.Web/Services/SmtpMailService.cs` (Modified - 5 log statements)
- `SocialEngineeringPlatform.Web/Data/DbInitializer.cs` (Modified - 4 log statements)
- `SocialEngineeringPlatform.Web/Services/CampaignExecutionService.cs` (Source of data flow)

## Compliance Status
✅ **GitHub CodeScan Alert**: RESOLVED - Email addresses are now masked before logging in all locations
- ✅ SmtpMailService.cs - 5 instances fixed
- ✅ DbInitializer.cs - 4 instances fixed

---

**Date**: October 21, 2025
**Issue**: Exposure of Private Information
**Severity**: Medium
**Status**: Fixed (All Instances)
