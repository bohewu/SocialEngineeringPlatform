# 系統設計文件（System Design）

專案：SocialEngineeringPlatform  
版本：v1.0  
日期：2025-10-21

---

## 1. 整體架構（Architecture Overview）

- Web：ASP.NET Core Razor Pages
- 身份與授權：ASP.NET Core Identity（角色：Administrator、CampaignManager）
- 資料存取：EF Core（多供應者：Sqlite / SQL Server / PostgreSQL）
- 郵件：MailKit（`SmtpMailService`）
- 背景作業：
  - `ScheduledCampaignSender`（BackgroundService）定期掃描 Scheduled + IsAutoSend 活動
  - 可選用 Hangfire（Dashboard /hangfire，依 DB 供應者切換儲存）
- 追蹤端點：Pages `/TrackOpen`, `/TrackClick`, `/TrackLanding`, `/TrackSubmit`
- 健康檢查：/health
- 數據保護：DataProtection（SMTP 密碼加解密）

## 2. 模組設計（Modules）

- Data（`ApplicationDbContext`）
  - DbSet：TargetUsers, TargetGroups, MailTemplates, LandingPageTemplates, Campaigns, CampaignTargets, TrackingEvents, MailSendLogs, MailTemplateCategories, SmtpSettings
  - Fluent API 設定主鍵/索引/刪除行為/Enum 文字化
- Services
  - `SmtpMailService`：組裝 MimeMessage、連線/驗證、寄送、日誌（使用 `LoggingHelper.MaskEmail`）
  - `CampaignExecutionService`：載入活動、決定目標名單（群組或手動）、以 AngleSharp 產生追蹤 URL 與像素、呼叫 MailService 寄送、寫入 MailSendLog/更新 CampaignTarget
  - `DatabaseSettingsService`（介面 `ISettingsService`）：讀取 DbSmtpSetting
- BackgroundServices
  - `ScheduledCampaignSender`：每分鐘掃描符合條件的 Campaign Id，逐一呼叫 `CampaignExecutionService`
- Pages（Razor Pages）
  - 追蹤頁：TrackOpen/Click/Landing/Submit（匿名）
  - 其他頁面需授權，Identity 頁面允許匿名

## 3. 資料庫設計（Database Design）

- 主要實體與關聯（文字 ER 概述）：
  - Campaign 1..\* CampaignTarget（至 TargetUser）
  - Campaign 1..\* TrackingEvent、1..\* MailSendLog
  - Campaign -\* MailTemplate（1）、-\* LandingPageTemplate（0..1）
  - TargetUser -\* TargetGroup（0..1）
- 重要欄位摘要：
  - Campaign：Name、MailTemplateId、LandingPageTemplateId?、TargetGroupId?、ScheduledSendTime?、Status（字串儲存）、IsAutoSend、SendBatchDelaySeconds
  - CampaignTarget：（PK: CampaignId + TargetUserId）、SendStatus、SendTime、ErrorMessage
  - MailSendLog：Id、CampaignId、TargetUserId、SendTime、Status、ErrorMessage
  - TrackingEvent：Id、CampaignId、TargetUserId、EventType、EventTime、EventDetails
  - DbSmtpSetting：Host、Port、EnableSsl、Username、EncryptedPassword、FromAddress（必填）、FromDisplayName

## 4. 核心流程（Flow & Sequence）

### 4.1 活動執行（CampaignExecutionService）
 
1. 載入 Campaign（含 MailTemplate、Targets/Group）並檢核狀態（Draft/Scheduled 才可）
1. 依 TargetGroup 決定名單；若未指定群組則使用手動清單
1. 以 AngleSharp 解析 MailTemplate.Body：
   - 將 `#PHISHING_LINK#` 轉換為 `/Track/Landing?c={Cid}&t={Tid}`（若有 LandingPageTemplate）
   - 外部連結改寫為 `/Track/Click?...`（Base64 Urlsafe 編碼原始 URL）
   - 若 TrackOpens 開啟，插入追蹤像素 `/Track/Open?c=...&t=...`
1. 呼叫 `SmtpMailService.SendEmailAsync(to, subject, html, from, name)`
1. 寫入 MailSendLog、更新 CampaignTarget.SendStatus 與時間
1. 最後 SaveChanges，更新 Campaign 狀態與結果訊息

### 4.2 排程執行（ScheduledCampaignSender）
 
1. 每分鐘檢查 Scheduled + IsAutoSend = true 且時間到期的活動 Id 清單
1. 逐一呼叫 `ExecuteCampaignAsync(campaignId, baseUrl)`
1. 記錄成功/失敗訊息

### 4.3 追蹤端點（Track Pages）
 
- TrackOpen：記錄 EventType=Open
- TrackClick：解析 `url` 參數（Base64Url），記錄 EventType=Click 後 302 導向原連結
- TrackLanding：記錄 EventType=Landing（顯示對應的登入頁）
- TrackSubmit：記錄 EventType=Submit（接收使用者提交資料；需注意隱私）

## 5. 安全設計（Security）

- 身份與授權：Identity + 角色；Razor Pages 預設需登入（/Identity、/Error、/Privacy 允許匿名）
- Hangfire Dashboard：僅 Administrator 可存取
- 敏感資料：
  - SMTP 密碼以 DataProtection 加解密存於資料庫
  - 日誌中 Email 使用 `LoggingHelper.MaskEmail` 遮蔽；避免記錄 `SendAsync` 的結果物件
- XSS 與輸入處理：
  - jquery.validate 套件選項須文件化；若以使用者輸入生成 HTML，需先消毒/escape
  - Razor 自動 HTML 編碼；僅在必要時使用 `Html.Raw`
- CSRF：Razor Pages 內建防護（Anti-forgery）
- 傳輸安全：建議生產強制 HTTPS + HSTS
- 祕密管理：連線字串/金鑰使用環境變數或機密儲存

## 6. 組態與環境（Configuration）

- DatabaseProvider：`Sqlite` | `SqlServer` | `Postgres`（於 `Program.cs` 切換）
- ConnectionStrings：對應連線字串；Sqlite 支援相對路徑轉絕對
- AppSettings：`BaseUrl` 供追蹤 URL 使用
- DataProtection：可在生產環境持久化金鑰（檔案/Blob/Redis）
- Hangfire：依 DB 使用 Memory/SQLServer/PostgreSQL 儲存

## 7. 部署（Deployment）

- 建置：.NET 8（推測），Razor Pages 專案 `SocialEngineeringPlatform.Web`
- 資料庫：啟動時可透過 `DbInitializer.InitializeAsync` 寫入預設資料（角色與管理員）
- 遷移：使用 EF Core Migrations；確保連線字串與 DatabaseProvider 正確
- 環境：Development 啟用 MigrationsEndPoint；Production 啟用 ExceptionHandler + HSTS
- 擴展：調整 Hangfire Workers、使用反向代理/負載平衡

## 8. 可觀測性（Observability）

- 健康檢查：`/health`
- 日誌：避免寫出個資；採結構化（有佔位 `{}`）
- 寄送/追蹤 KPI：
  - Send Throughput（每分鐘）、Delivery/Fail Rate、Open/Click/Landing/Submit Rates
  - 以 MailSendLog、TrackingEvent 彙整
- 儀表板：Hangfire Dashboard 提供背景工作視覺化

## 9. 品質保證（Quality & Testing）

- 單元測試：`SmtpMailService`（SMTP 錯誤處理）、`CampaignExecutionService`（名單/追蹤改寫）
- 整合測試：追蹤端點（Click 轉導、Open 像素）、KPI 查詢
- 安全測試：日誌資料洩漏、XSS 注入、權限繞過

## 10. 延伸規劃（Roadmap）

- 真實退信（Bounce/Complaint）Webhook 整合
- 報表與匯出（CSV/Excel）、可視化儀表板
- 多租戶（Tenant）隔離、活動 AB 測試
- 範本版本化、內容審核流程
- 更細緻的速率限制與併發控制

---

（本文件將隨版本演進持續更新）
