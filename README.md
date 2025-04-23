# **社交工程演練平台**

🚨 **實驗性質警告 (Experimental Status Warning)** 🚨

**請注意：** 本專案目前仍處於**實驗性質**階段。雖然已實作多項核心功能，但可能仍存在未知的錯誤、漏洞或未完善之處。**請勿在未經充分測試和風險評估的情況下，直接將其用於生產環境或針對真實使用者進行大規模演練。** 使用者應自行承擔使用本軟體可能帶來的所有風險。

## **專案簡介**

本專案旨在建立一個內部使用的社交工程演練平台，用於規劃、執行、追蹤和分析模擬的釣魚郵件攻擊，以評估組織成員的安全意識，並提供數據洞察以強化整體安全防護。

## **主要功能**

* **儀表板 (Dashboard):** 提供系統概觀，包含活動總數、各狀態活動統計、最近活動列表等。  
* **演練活動管理 (Campaigns):**  
  * 建立、編輯、刪除演練活動。  
  * 設定活動名稱、描述。  
  * 關聯郵件範本。  
  * (可選) 關聯登陸頁範本。  
  * (可選) 指定單一目標群組。  
  * 設定排程發送時間與是否自動發送。  
  * 設定批次發送間隔。  
  * 設定是否追蹤郵件開啟。  
  * 手動立即發送活動郵件。  
  * 手動結束進行中或排程中的活動。  
* **目標管理 (Targets):**  
  * **目標群組管理:** 建立、讀取、編輯、刪除目標群組。  
  * **目標使用者管理:** 建立、讀取、編輯、刪除目標使用者，包含 Email、姓名、所屬群組、自訂欄位等。  
  * **匯入目標使用者:** 支援從 CSV 檔案批次匯入使用者資料，提供樣板下載與錯誤回饋。  
  * **管理活動目標:** 為未指定目標群組的活動手動新增或移除個別目標使用者。  
* **範本管理 (Templates):**  
  * **郵件範本管理:** 建立、讀取、編輯、刪除郵件範本，支援 Rich Text Editor (TinyMCE) 編輯 HTML 內文，可設定分類、語言、自訂寄件者資訊。  
  * **登陸頁範本管理:** 建立、讀取、編輯、刪除模擬登陸頁範本，支援 HTML 內容編輯，可設定收集欄位資訊。  
  * **範本分類管理:** 建立、讀取、編輯、刪除郵件範本分類。  
* **追蹤與記錄 (Tracking):**  
  * **郵件開啟追蹤:** (可選) 使用追蹤像素記錄郵件開啟事件 (準確性受限)。  
  * **連結點擊追蹤:** 將郵件內連結替換為追蹤連結，記錄點擊事件並重新導向。  
  * **登陸頁訪問與提交追蹤:** 記錄使用者訪問登陸頁（視為點擊），並記錄表單提交動作（不記錄敏感內容）。  
  * 記錄郵件發送日誌 (MailSendLogs)。  
  * 記錄追蹤事件 (TrackingEvents)。  
* **自動化排程 (Automation):**  
  * 整合 **Hangfire** 處理背景排程任務。  
  * 根據活動設定的排程時間自動觸發郵件發送。  
  * 提供 Hangfire 儀表板 (/hangfire) 監控背景工作。  
* **系統管理 (Admin):**  
  * **角色管理:** 檢視、建立、刪除系統角色 (例如 Administrator, CampaignManager)。防止刪除仍有使用者歸屬的角色。  
  * **使用者管理:** 檢視、建立、編輯系統使用者帳號資訊，管理使用者角色指派，提供帳號鎖定/解鎖功能。防止管理員修改自身關鍵狀態或刪除自身帳號。  
  * **系統設定:** 提供介面管理 SMTP 伺服器設定（儲存於資料庫，密碼加密）。  
* **安全性:**  
  * 使用 ASP.NET Core Identity 進行使用者驗證與授權。  
  * 限制管理頁面僅特定角色（如 Administrator）可訪問。  
  * 使用 HTTPS。  
  * 資料庫密碼加密儲存。

## **技術棧**

* **後端:** ASP.NET Core 9 (Razor Pages), Entity Framework Core 9  
* **資料庫:** SQL Server (LocalDB for Development)  
* **前端:** Bootstrap 5, jQuery, DataTables.net  
* **郵件:** MailKit  
* **HTML 編輯:** TinyMCE (GPLv2+)  
* **背景排程:** Hangfire (LGPL 3.0)  
* **HTML 解析:** AngleSharp (MIT License)  
* **CSV 處理:** CsvHelper (MS-PL / Apache 2.0)

## **設定與執行**

1. 確保已安裝 **.NET 9 SDK**。  
2. 複製專案。  
3. **設定初始管理員帳號 (重要！)：**  
   * **開發環境：** 建議使用 **User Secrets** 來儲存初始管理員的 Email 和密碼。在專案目錄執行以下指令 (將 your\_admin\_email 和 your\_strong\_password 替換為實際值)：  
     dotnet user-secrets init  
     dotnet user-secrets set "AppSettings:AdminUser:Email" "your\_admin\_email@example.com"  
     dotnet user-secrets set "AppSettings:AdminUser:Password" "your\_strong\_password"

   * **生產環境：** **切勿**將密碼寫在 appsettings.json 中。請使用**環境變數**、**Azure Key Vault** 或其他安全的組態提供者來設定 AppSettings:AdminUser:Email 和 AppSettings:AdminUser:Password。  
   * **設定其他組態：** 在 appsettings.Development.json (或對應環境的設定檔) 中：  
     * 設定 ConnectionStrings:DefaultConnection 指向您的 SQL Server / LocalDB。  
     * 設定 AppSettings:BaseUrl 為您應用程式部署後的公開網址（用於追蹤連結）。  
4. 開啟終端機，進入專案資料夾 (SocialEngineeringPlatform.Web)。  
5. **首次建立資料庫與套用結構：**  
   * **(重要)** 如果您是第一次設定此專案，或已移除 Migrations 資料夾，請先執行以下指令產生初始遷移檔案：  
     dotnet ef migrations add InitialCreate

     *(您可以將 InitialCreate 替換為其他描述性的名稱)*  
   * 然後，執行以下指令來建立資料庫（如果不存在）並套用遷移：  
     dotnet ef database update

6. 執行專案：  
   dotnet run

7. 使用您在步驟 3 中設定的初始管理員帳號登入，並前往「系統設定」頁面設定正確的 SMTP 伺服器資訊。  
8. (可選) 瀏覽 /hangfire 查看背景工作儀表板（需要管理員權限）。

## **授權**

本專案依據 **GNU General Public License (GPL) 第 2 版或您選擇的任何更新版本**進行授權。詳情請參閱 LICENSE 檔案。

## **免責聲明 (Warranty Disclaimer)**

**本軟體按「原樣」提供，不附帶任何形式的明示或暗示保證，包括但不限於對適銷性、特定目的適用性和非侵權性的暗示保證。您將自行承擔使用本軟體的全部風險。**

## **責任限制 (Limitation of Liability)**

**在任何情況下，除非適用法律要求或書面同意，否則本軟體的任何版權持有人或任何修改和/或重新分發本軟體的其他方，均不對您的任何直接、間接、偶然、特殊、懲罰性或後果性損害負責（包括但不限於替代商品或服務的採購；使用、資料或利潤的損失；或業務中斷），無論此類損害是如何造成的，以及基於何種責任理論（無論是合約責任、嚴格責任或侵權行為（包括過失或其他原因）），即使已被告知發生此類損害的可能性。使用本軟體即表示您同意放棄追究上述責任的權利。**

## **Credits**

本專案的開發過程大量借助了大型語言模型 **Gemini 2.5 Pro (Google)** 的協助。其貢獻包括但不限於：

* **程式碼撰寫：** 貢獻了超過 90% 的 C\# 後端邏輯、Razor Page 頁面、HTML、CSS 及 JavaScript 程式碼。  
* **架構設計：** 提供了專案結構、資料庫設計 (ERD)、實作順序規劃等建議。  
* **功能實作：** 協助完成了大部分核心功能，如 CRUD 操作、郵件發送、點擊/開啟/提交追蹤、背景排程、使用者/角色管理、資料匯入、報表頁面等。  
* **問題除錯：** 協助診斷和修復了開發過程中遇到的多個錯誤和 Bug。  
* **文件產出：** 產生了包括需求整理、實作規劃、授權聲明及本 README 文件在內的多份文件。

This project was developed with significant assistance from the large language model Gemini 2.5 Pro (Google). Its contributions include, but are not limited to:

\* \*\*Code Writing:\*\* Contributed over 90% of the C\# backend logic, Razor Page views, HTML, CSS, and JavaScript code.  
\* \*\*Architectural Design:\*\* Provided suggestions for project structure, database design (ERD), and implementation planning.  
\* \*\*Feature Implementation:\*\* Assisted in completing most core features, such as CRUD operations, email sending, click/open tracking, background scheduling, user/role management, data import, report pages, etc.  
\* \*\*Debugging:\*\* Helped diagnose and fix multiple errors and bugs encountered during development.  
\* \*\*Documentation:\*\* Generated various documents, including requirements gathering, implementation plans, license statements, and this README file.  
