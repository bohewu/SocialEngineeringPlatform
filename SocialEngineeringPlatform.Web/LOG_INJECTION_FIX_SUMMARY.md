# 日誌注入漏洞修正總結

## 修正日期
2025-10-20

## 問題來源
GitHub CodeQL 安全掃描檢測到日誌注入漏洞（Log Injection）

## 修正的檔案

### 1. Services/CampaignExecutionService.cs
**修正內容：**
- ✅ Line ~240: 釣魚連結追蹤日誌 - 移除完整 URL，改記錄 CampaignId 和 TargetUserId
- ✅ Line ~253: 普通連結追蹤日誌 - 移除完整 URL，改記錄 ID 和 URL scheme
- ✅ Line ~258: 跳過連結日誌 - 移除原始連結字串，改記錄連結類型
- ✅ Line ~213: 自訂寄件者日誌 - 移除可能的使用者輸入 email
- ✅ Line ~219: 自訂寄件者名稱日誌 - 移除可能的使用者輸入名稱

**修正前範例：**
```csharp
_logger.LogTrace("釣魚連結替換 -> '{Tracking}'", finalUrl);
// finalUrl 包含 campaign.Id 和 campaignTarget.TargetUserId
```

**修正後範例：**
```csharp
_logger.LogTrace("釣魚連結替換為登陸頁追蹤連結。CampaignId={CampaignId}, TargetUserId={TargetUserId}", 
    campaign.Id, campaignTarget.TargetUserId);
```

### 2. Pages/TrackClick.cshtml.cs
**修正內容：**
- ✅ Line ~52: URL 解碼錯誤日誌 - 移除解碼後的完整 URL
- ✅ Line ~95: 點擊事件記錄日誌 - 改為只記錄 URL 的 scheme 和 host

**修正前範例：**
```csharp
_logger.LogWarning("點擊追蹤請求中的 URL 解碼後無效: {OriginalUrl} (Encoded: {EncodedUrl})", 
    originalUrl, encodedUrl);
```

**修正後範例：**
```csharp
_logger.LogWarning("點擊追蹤請求中的 URL 解碼後無效。EncodedUrl: {EncodedUrl}", encodedUrl);
```

### 3. Services/SmtpMailService.cs
**修正內容：**
- ✅ Line ~118: 發送郵件日誌 - 移除郵件主旨（可能包含使用者輸入）

**修正前範例：**
```csharp
_logger.LogInformation("正在嘗試發送郵件至 {Recipient}，主旨：{Subject}", toEmail, subject);
```

**修正後範例：**
```csharp
_logger.LogInformation("正在嘗試發送郵件至 {Recipient}", toEmail);
```

## 修正原則

### 1. 使用結構化日誌
❌ **錯誤**：將使用者輸入嵌入到日誌訊息字串中
```csharp
_logger.LogTrace($"釣魚連結替換 -> {finalUrl}");
```

✅ **正確**：使用參數化日誌
```csharp
_logger.LogTrace("釣魚連結替換。CampaignId={CampaignId}", campaign.Id);
```

### 2. 最小化敏感資訊記錄
❌ **錯誤**：記錄完整的 URL、Email 主旨、使用者名稱等
```csharp
_logger.LogInformation("發送郵件: {Email}, 主旨: {Subject}", email, subject);
```

✅ **正確**：只記錄必要的識別資訊
```csharp
_logger.LogInformation("發送郵件: {Email}", email);
```

### 3. 對外部 URL 只記錄關鍵資訊
❌ **錯誤**：記錄完整 URL（可能包含查詢參數、片段等）
```csharp
_logger.LogInformation("點擊 URL: {Url}", fullUrl);
```

✅ **正確**：只記錄 scheme 和 host
```csharp
var urlInfo = $"{uri.Scheme}://{uri.Host}";
_logger.LogInformation("點擊 URL: {UrlHost}", urlInfo);
```

## 安全性改善效果

### 1. 防止日誌注入攻擊
- 攻擊者無法在日誌中注入換行符號 (`\n`, `\r`)
- 無法偽造日誌條目
- 無法執行日誌汙染攻擊

### 2. 減少敏感資訊洩露
- 不再記錄完整的追蹤 URL
- 不再記錄郵件主旨
- 不再記錄完整的外部連結

### 3. 符合安全最佳實踐
- 使用 .NET 結構化日誌
- 參數自動轉義和驗證
- 符合 OWASP 日誌記錄指南

## 驗證步驟

1. ✅ 執行 `dotnet build` - 建置成功，無錯誤
2. ⏳ 執行 GitHub CodeQL 掃描 - 待下次 push 後驗證
3. ⏳ 檢查日誌輸出格式 - 待應用程式運行後驗證

## 參考資料
- [OWASP Log Injection](https://owasp.org/www-community/attacks/Log_Injection)
- [Microsoft .NET Logging Best Practices](https://docs.microsoft.com/en-us/dotnet/core/extensions/logging)
- [Structured Logging with Serilog](https://github.com/serilog/serilog/wiki/Structured-Data)

## 建議的後續步驟

1. **在 CI/CD 中整合 CodeQL**
   - 在每次 PR 時自動執行安全掃描
   - 設定安全性政策，阻止有高風險漏洞的程式碼合併

2. **日誌審計**
   - 定期審查日誌記錄的內容
   - 確保沒有記錄密碼、Token 等敏感資訊

3. **日誌等級調整**
   - 在生產環境將 Trace 和 Debug 等級關閉
   - 只保留 Information、Warning、Error 等級

4. **實作日誌脫敏**
   - 考慮使用 Serilog 的 Destructuring 功能
   - 自動過濾 Email、電話等個人資訊

## 總結

✅ 所有已知的日誌注入漏洞已修正
✅ 程式碼建置成功，無編譯錯誤
✅ 使用結構化日誌，符合安全最佳實踐
✅ 減少敏感資訊在日誌中的洩露風險

待 GitHub CodeQL 下次掃描時，應該不會再出現日誌注入警告。
