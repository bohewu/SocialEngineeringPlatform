# 系統規格文件（System Specification）

專案：SocialEngineeringPlatform  
版本：v1.0  
日期：2025-10-21

---

## 1. 簡介（Overview）
 
SocialEngineeringPlatform 是一個釣魚演練與社工測試平台，提供活動（Campaign）建立、郵件範本與登入頁範本管理、目標使用者與群組管理、排程與自動寄送、開啟/點擊/登入/提交等行為追蹤，以及寄送結果與統計報表。本系統以 ASP.NET Core Razor Pages 為基礎，資料存取使用 EF Core，身份管理使用 ASP.NET Core Identity，郵件寄送採 MailKit，背景作業可由 BackgroundService 或 Hangfire 執行。

## 2. 範疇（Scope）
 
- In Scope
  - 活動管理：建立、排程、執行、結果紀錄
  - 郵件範本與登入頁範本管理
  - 目標使用者與群組管理
  - 郵件寄送與追蹤（開啟、點擊、登入、提交）
  - 儀表板與統計（寄送數、成功/失敗、行為率、活動狀態）
  - 角色權限與儀表板（Hangfire Dashboard 受權）
  - 多資料庫供應者（Sqlite / SQL Server / PostgreSQL）
- Out of Scope（目前）
  - 真實郵件退信（Bounce）回饋處理（僅以寄送失敗近似）
  - 外部報表平台整合（提供可延伸介面）

## 3. 利害關係人（Stakeholders）
 
- 系統管理員（Administrator）：系統設定、角色與使用者治理、SMTP 與安全設定、Hangfire 儀表板存取
- 活動經理（Campaign Manager）：活動建立與維運、範本/群組/目標管理、結果分析
- 稽核/資訊安全人員：審閱記錄、合規與隱私驗證
- 受測對象（Target User）：接收郵件並可能觸發追蹤事件

## 4. 名詞定義（Glossary）
 
- 活動 Campaign：一次社工演練，包含寄送對象、郵件/登入頁範本、排程與追蹤設定
- 目標使用者 TargetUser：被寄送郵件的受測者
- 目標群組 TargetGroup：受測者分群
- 郵件範本 MailTemplate：郵件主旨與 HTML 內容（可含釣魚連結占位符）
- 登入頁範本 LandingPageTemplate：模擬登入/提交頁用於收集互動
- 追蹤事件 TrackingEvent：Open/Click/Landing/Submit 等行為紀錄
- 寄送紀錄 MailSendLog：每封郵件的寄送狀態與時間

## 5. KPI 指標（含定義與公式）
 
- 活動寄送總數（Attempts）= 該活動之 MailSendLog 總筆數
- 寄達率（Delivery Rate）= 成功寄送數 / 寄送嘗試數  
  - 成功寄送數= MailSendLog.Status = Sent  
  - 失敗近似= Status = Failed（不含真實 SMTP bounce 回饋）
- 開啟率（Open Rate）= 唯一開啟人數 / 成功寄送數  
  - 唯一開啟人數= TrackingEvent(EventType=Open) 以 TargetUserId 去重
- 點擊率（CTR）= 唯一點擊人數 / 成功寄送數  
  - 唯一點擊= TrackingEvent(EventType=Click) 去重
- 登入/到達率（Landing Rate）= 唯一 Landing 人數 / 成功寄送數
- 提交率（Submit Rate）= 唯一 Submit 人數 / 成功寄送數
- 轉換漏斗（Funnel）= Delivered → Open → Click → Landing → Submit 轉換各階段比
- 失敗率（Send Failure Rate）= 失敗寄送數 / 寄送嘗試數
- 寄送吞吐量（Send Throughput）= 單位時間（分鐘）寄送的郵件數（MailSendLog 分組計算）
- 活動完成時間（Campaign Lead Time）= ActualStartTime → 最後一筆 MailSendLog.SendTime 的時間差
- 目標名單成長（Target Growth）= 期初/期末 TargetUser 計數的差

（可依需求以 SQL/EF 查詢實作；例如開啟率以 TrackingEvent.EventType='Open' group by TargetUserId）

## 6. 系統用例（Use Cases）
 
 1. 管理 SMTP 設定（Admin）
    - 建立/更新資料庫中的 SMTP 設定（DbSmtpSetting），密碼以 DataProtection 加密
    - 測試寄送
 
 1. 管理郵件範本（Campaign Manager）
    - 新增/編輯/刪除 MailTemplate，支援自訂 From/DisplayName
 
 1. 管理登入頁範本（Campaign Manager）
    - 新增/編輯/刪除 LandingPageTemplate，配置收集欄位
 
 1. 目標與群組管理（Campaign Manager）
    - 匯入/新增 TargetUser，建立 TargetGroup 並加入成員
 
 1. 建立與排程活動（Campaign Manager）
    - 指定 MailTemplate、（可選）LandingPageTemplate、（可選）TargetGroup、是否自動寄送與排程時間
 
 1. 執行活動（自動/手動）
    - 由 BackgroundService 或排程器挑選 Campaign，呼叫 CampaignExecutionService 寄送並寫入 MailSendLog、更新 CampaignTarget 狀態
 
 1. 追蹤互動
    - /TrackOpen /TrackClick /TrackLanding /TrackSubmit 頁面受匿名呼叫，寫入 TrackingEvent
 
 1. 檢視報表/儀表板
    - 依活動查詢 KPI 與明細
 
 1. 系統治理（Admin）
    - 身份/角色管理、Hangfire Dashboard 存取

## 7. 功能需求（Functional Requirements）
 
- FR-001 建立/編輯/刪除 活動、郵件範本、登入頁範本、目標群組、目標使用者
- FR-002 支援以群組或手動清單決定寄送對象
- FR-003 支援活動排程與自動寄送（Scheduled + IsAutoSend）
- FR-004 郵件寄送（MailKit）與結果紀錄（MailSendLog）
- FR-005 追蹤事件：Open、Click、Landing、Submit（TrackingEvent）
- FR-006 儀表板與報表：按活動顯示 KPI 與明細
- FR-007 角色權限：Administrator、CampaignManager，保護 Hangfire Dashboard
- FR-008 設定管理：DbSmtpSetting、AppSettings(BaseUrl)

## 8. 非功能需求（Non-Functional Requirements）
 
- 安全
  - 使用者身份與授權：ASP.NET Core Identity + 角色
  - 敏感資訊保護：DataProtection 加密 SMTP 密碼；日誌遮蔽 Email，避免記錄 SMTP 結果載荷
  - XSS 防護：前端套件選項需文件化與輸入消毒（jquery.validate 建議）
  - 強制 HTTPS、HSTS（生產）
- 隱私
  - 日誌不記錄個資（Email 以遮蔽函式處理）
  - 追蹤資料遵循最小化原則（EventDetails 僅存必要資訊）
- 可用性/擴充
  - 多資料庫供應者切換（Sqlite/SQL Server/PostgreSQL）
  - 背景作業可改用 Hangfire，具備儀表板
- 效能
  - 可調整 WorkerCount、批次延遲；KPI 監測吞吐量
- 可維運
  - 健康檢查 /health、Hangfire Dashboard、集中式日誌

## 9. 資料模型與資料字典（重點）
 
- ApplicationUser：Identity 擴充，導航至 CreatedCampaigns/Templates/LandingPages
- Campaign：Name, Description, MailTemplateId, LandingPageTemplateId?, TargetGroupId?, ScheduledSendTime?, Status, IsAutoSend, SendBatchDelaySeconds, Create/UpdateTime, CreatedByUserId
- CampaignTarget：(CampaignId, TargetUserId) PK，SendStatus，SendTime，ErrorMessage
- TargetUser：Email, Name, GroupId?, CustomField1/2, IsActive, Create/UpdateTime
- TargetGroup：Name, Description, Create/UpdateTime
- MailTemplate：Name, Subject, Body, Language?, CategoryId?, AttachmentPath?, CustomFromAddress?, CustomFromDisplayName?, Create/UpdateTime, CreatedByUserId
- LandingPageTemplate：Name, HtmlContent, CollectFieldsConfig?, Create/UpdateTime, CreatedByUserId
- TrackingEvent：Id, CampaignId, TargetUserId, MailTemplateId?, LandingPageTemplateId?, EventTime, EventType, EventDetails?
- MailSendLog：Id, CampaignId, TargetUserId, SendTime, Status, SmtpServerUsed?, ErrorMessage?
- DbSmtpSetting：Host, Port, EnableSsl, Username, EncryptedPassword, FromAddress, FromDisplayName

（關聯：Campaign 1..\* CampaignTarget；CampaignTarget -\*..1 TargetUser；Campaign 1..\* MailSendLog、1..\* TrackingEvent；MailTemplate / LandingPageTemplate 與 Campaign、TrackingEvent 關聯）

## 10. 權限矩陣（簡要）
 
- Administrator：
  - 系統設定（DbSmtpSetting、AppSettings）、角色/使用者治理、Hangfire Dashboard、所有資料 CRUD
- CampaignManager：
  - 活動/範本/目標/群組 CRUD、啟動與檢視報表
- 匿名：
  - 追蹤端點（/TrackOpen/Click/Landing/Submit）

## 11. 外部介面與整合
 
- SMTP：MailKit 寄送
- Hangfire：Dashboard（/hangfire）+ 儲存選擇（Memory/SQL Server/PostgreSQL 依環境）
- 健康檢查：/health

## 12. 設定項目（Configuration）
 
- appsettings.json / 環境變數：
  - DatabaseProvider：Sqlite | SqlServer | Postgres
  - ConnectionStrings：SqliteConnection, SqlServerConnection, PostgresConnection
  - AppSettings.BaseUrl：追蹤 URL 的 BaseUrl
  - AdminUser（可選）：預設管理員 Email/Password（由 DbInitializer 使用）
- 資料庫設定（DbSmtpSetting）：FromAddress 必填，密碼以 DataProtection 加密

## 13. 風險與限制（Risks & Constraints）
 
- 無真實退信整合 → 失敗率為近似值
- 若未妥善遮蔽日誌/輸入消毒，存在隱私與 XSS 風險
- 大量名單寄送需規劃節流、重試與併發控制

## 14. 測試與驗收（Acceptance）
 
- 單元/整合測試：寄送服務、活動執行服務、追蹤端點
- UAT：以測試名單完整跑一次活動，驗證 KPI 計算與報表
- 安全檢測：日誌不含個資（Email 遮蔽）、追蹤端點不洩漏敏感資訊

---

（本文件將隨版本演進持續更新）

---

## 15. 附錄：KPI 計算範例（EF Core 與 SQL）

以下以單一活動 CampaignId = {campaignId} 為例（請於實作時以參數化方式帶入）：

### 15.1 寄達率（Delivery Rate）

EF Core（C#）：

```csharp
var attempts = await _context.MailSendLogs
    .CountAsync(x => x.CampaignId == campaignId);
var delivered = await _context.MailSendLogs
    .CountAsync(x => x.CampaignId == campaignId && x.Status == CampaignSendStatus.Sent);
var deliveryRate = attempts == 0 ? 0 : (double)delivered / attempts;
```

SQL（T-SQL/PostgreSQL 類似）：

```sql
SELECT
  COUNT(*)                           AS attempts,
  SUM(CASE WHEN Status = 'Sent' THEN 1 ELSE 0 END) AS delivered,
  CASE WHEN COUNT(*) = 0 THEN 0
       ELSE 1.0 * SUM(CASE WHEN Status = 'Sent' THEN 1 ELSE 0 END) / COUNT(*)
  END AS delivery_rate
FROM MailSendLogs
WHERE CampaignId = @CampaignId;
```

### 15.2 開啟率（Open Rate）

EF Core（C#）：

```csharp
var deliveredCount = delivered; // 延用上節計算
var uniqueOpens = await _context.TrackingEvents
    .Where(e => e.CampaignId == campaignId && e.EventType == TrackingEventType.Open)
    .Select(e => e.TargetUserId)
    .Distinct()
    .CountAsync();
var openRate = deliveredCount == 0 ? 0 : (double)uniqueOpens / deliveredCount;
```

SQL：

```sql
WITH delivered AS (
  SELECT COUNT(*) AS delivered
  FROM MailSendLogs
  WHERE CampaignId = @CampaignId AND Status = 'Sent'
), opens AS (
  SELECT COUNT(DISTINCT TargetUserId) AS unique_opens
  FROM TrackingEvents
  WHERE CampaignId = @CampaignId AND EventType = 'Open'
)
SELECT CASE WHEN d.delivered = 0 THEN 0
            ELSE 1.0 * o.unique_opens / d.delivered END AS open_rate
FROM delivered d CROSS JOIN opens o;
```

### 15.3 點擊率（CTR）與轉換漏斗（Funnel）

EF Core（C#）：

```csharp
var uniqueClicks = await _context.TrackingEvents
    .Where(e => e.CampaignId == campaignId && e.EventType == TrackingEventType.Click)
    .Select(e => e.TargetUserId)
    .Distinct()
    .CountAsync();
var clickRate = deliveredCount == 0 ? 0 : (double)uniqueClicks / deliveredCount;
```

SQL（同理可求 Landing/Submit）：

```sql
SELECT COUNT(DISTINCT TargetUserId) AS unique_clicks
FROM TrackingEvents
WHERE CampaignId = @CampaignId AND EventType = 'Click';
```

### 15.4 吞吐量（Throughput，依分鐘）

SQL：

```sql
SELECT
  DATEADD(MINUTE, DATEDIFF(MINUTE, 0, SendTime), 0) AS minute_bucket,
  COUNT(*) AS sent_count
FROM MailSendLogs
WHERE CampaignId = @CampaignId AND Status = 'Sent'
GROUP BY DATEADD(MINUTE, DATEDIFF(MINUTE, 0, SendTime), 0)
ORDER BY minute_bucket;
```

（PostgreSQL 可用 date_trunc('minute', SendTime) 實作）

