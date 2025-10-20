# 安全性修正記錄

## 2025-10-20: 修正日誌注入漏洞 (Log Injection)

### 問題描述
GitHub CodeQL 安全掃描發現在 `CampaignExecutionService.cs` 中存在日誌注入漏洞：
- **問題位置**: 第 240 行（修正前）
- **問題類型**: Log entries created from user input
- **風險等級**: 中等
- **影響範圍**: 日誌記錄可能被惡意使用者注入任意內容

### 根本原因
程式碼直接將包含使用者輸入的完整 URL 記錄到日誌中：
```csharp
// 不安全的寫法
_logger.LogTrace("釣魚連結替換 -> '{Tracking}'", finalUrl);
```

其中 `finalUrl` 包含了 `campaign.Id` 和 `campaignTarget.TargetUserId`，這些參數來自資料庫，可能被惡意使用者操控。

### 修正方式
使用結構化日誌（Structured Logging），將使用者輸入作為參數傳遞，而不是直接嵌入到訊息字串中：

#### 修正前
```csharp
finalUrl = $"{baseUrl}/Track/Landing?c={campaign.Id}&t={campaignTarget.TargetUserId}";
_logger.LogTrace("釣魚連結替換 -> '{Tracking}'", finalUrl);
```

#### 修正後
```csharp
finalUrl = $"{baseUrl}/Track/Landing?c={campaign.Id}&t={campaignTarget.TargetUserId}";
_logger.LogTrace("釣魚連結替換為登陸頁追蹤連結。CampaignId={CampaignId}, TargetUserId={TargetUserId}", 
    campaign.Id, campaignTarget.TargetUserId);
```

### 修正的程式碼位置
- **檔案**: `Services/CampaignExecutionService.cs`
- **修正行數**:
  - Line ~240: 釣魚連結追蹤日誌
  - Line ~252: 普通連結追蹤日誌
  - Line ~258: 跳過連結日誌
  - Line ~213: 自訂寄件者日誌
  - Line ~219: 自訂寄件者名稱日誌

### 安全性改善
1. **結構化日誌**: 使用 `{參數名稱}` 語法，確保參數值被正確轉義
2. **最小化資訊洩露**: 不再記錄完整的 URL，只記錄必要的 ID 和類型資訊
3. **防止注入攻擊**: 惡意使用者無法在日誌中注入換行符號或其他控制字元

### 驗證方式
1. 執行 `dotnet build` 確認編譯成功
2. 執行 CodeQL 掃描確認警告已消除
3. 檢查日誌輸出格式是否正確

### 參考資料
- [OWASP Log Injection](https://owasp.org/www-community/attacks/Log_Injection)
- [Microsoft Logging Best Practices](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging/)
- [Structured Logging in .NET](https://docs.microsoft.com/en-us/dotnet/core/extensions/logging)

## 建議的後續改善

### 1. 檢查其他日誌記錄
搜尋專案中所有包含使用者輸入的日誌記錄，確保都使用結構化日誌：
```bash
# 搜尋可能的問題日誌
grep -r "LogTrace\|LogDebug\|LogInformation\|LogWarning\|LogError" --include="*.cs" | grep "{"
```

### 2. 啟用日誌過濾
在 `appsettings.json` 中設定適當的日誌等級，避免在生產環境記錄過多敏感資訊：
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "SocialEngineeringPlatform.Web.Services": "Information"
    }
  }
}
```

### 3. 使用日誌脫敏（Log Sanitization）
考慮實作自訂的日誌提供者，自動過濾敏感資訊（如 Email、ID 等）。

### 4. 定期安全掃描
- 在 CI/CD 流程中整合 CodeQL 或其他 SAST 工具
- 定期執行安全性掃描並修正發現的問題
- 啟用 GitHub Dependabot 自動更新依賴套件

## 檢查清單

- [x] 修正 `CampaignExecutionService.cs` 中的日誌注入問題
- [x] 使用結構化日誌記錄
- [x] 移除直接記錄完整 URL 的程式碼
- [x] 建置並測試修正後的程式碼
- [ ] 執行 CodeQL 掃描確認警告消除
- [ ] 檢查專案中其他可能的日誌安全問題
- [ ] 更新日誌等級設定
- [ ] 撰寫單元測試驗證日誌輸出格式
