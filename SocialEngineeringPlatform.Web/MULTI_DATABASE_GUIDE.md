# 多資料庫支援配置指南

## 概述

本專案已配置為支援多種資料庫提供者，可透過 `appsettings.json` 輕鬆切換：
- ✅ **SQLite** (預設，適合開發與小型部署)
- ✅ **SQL Server** (適合企業環境)
- ✅ **PostgreSQL** (開源，高效能)

## 如何切換資料庫

### 方法一：修改 `appsettings.json` (推薦)

編輯 `appsettings.json` 或 `appsettings.Development.json`，更改 `DatabaseProvider` 值：

```json
{
  "DatabaseProvider": "Sqlite",  // 選項: "Sqlite", "SqlServer", "Postgres"
  "ConnectionStrings": {
    "SqliteConnection": "Data Source=SocialEngineeringDB.db",
    "SqlServerConnection": "Server=(localdb)\\mssqllocaldb;Database=SocialEngineeringDB;Trusted_Connection=True;MultipleActiveResultSets=true",
    "PostgresConnection": "Host=localhost;Database=SocialEngineeringDB;Username=postgres;Password=postgres"
  }
}
```

### 方法二：環境變數覆蓋

透過環境變數設定 (適合容器化部署)：

```bash
# PowerShell
$env:DatabaseProvider="SqlServer"

# Linux/macOS
export DatabaseProvider=SqlServer
```

## 資料庫配置詳細說明

### 1. SQLite (預設)

**優點：**
- ✅ 零配置，無需安裝資料庫伺服器
- ✅ 資料庫檔案與應用程式一起移動
- ✅ 適合開發、測試、小型部署

**配置：**
```json
{
  "DatabaseProvider": "Sqlite",
  "ConnectionStrings": {
    "SqliteConnection": "Data Source=SocialEngineeringDB.db"
  }
}
```

**資料庫位置：**
- 開發環境：`bin/Debug/net9.0/SocialEngineeringDB.db`
- 生產環境：與執行檔同目錄

**注意事項：**
- ⚠️ 不適合高並發寫入場景
- ⚠️ 建議定期備份 `.db` 檔案

### 2. SQL Server

**優點：**
- ✅ 企業級資料庫，強大的 ACID 支援
- ✅ 豐富的管理工具
- ✅ 適合大型應用與高並發

**配置：**
```json
{
  "DatabaseProvider": "SqlServer",
  "ConnectionStrings": {
    "SqlServerConnection": "Server=YOUR_SERVER;Database=SocialEngineeringDB;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True"
  }
}
```

**連線字串範例：**
```
// LocalDB (開發)
Server=(localdb)\\mssqllocaldb;Database=SocialEngineeringDB;Trusted_Connection=True;MultipleActiveResultSets=true

// SQL Server Express
Server=localhost\\SQLEXPRESS;Database=SocialEngineeringDB;Trusted_Connection=True;MultipleActiveResultSets=true

// 遠端 SQL Server
Server=192.168.1.100,1433;Database=SocialEngineeringDB;User Id=sa;Password=YourPassword;TrustServerCertificate=True

// Azure SQL Database
Server=tcp:yourserver.database.windows.net,1433;Database=SocialEngineeringDB;User ID=yourusername;Password=yourpassword;Encrypt=True
```

### 3. PostgreSQL

**優點：**
- ✅ 開源免費，高效能
- ✅ 強大的 JSON/JSONB 支援
- ✅ 適合複雜查詢與分析

**配置：**
```json
{
  "DatabaseProvider": "Postgres",
  "ConnectionStrings": {
    "PostgresConnection": "Host=localhost;Database=SocialEngineeringDB;Username=postgres;Password=postgres"
  }
}
```

**連線字串範例：**
```
// 本地 PostgreSQL
Host=localhost;Database=SocialEngineeringDB;Username=postgres;Password=postgres

// 遠端 PostgreSQL
Host=192.168.1.100;Port=5432;Database=SocialEngineeringDB;Username=admin;Password=YourPassword

// PostgreSQL with SSL
Host=postgres.example.com;Port=5432;Database=SocialEngineeringDB;Username=user;Password=pass;SSL Mode=Require

// 使用連線池
Host=localhost;Database=SocialEngineeringDB;Username=postgres;Password=postgres;Pooling=true;Minimum Pool Size=5;Maximum Pool Size=100
```

## 資料庫遷移 (Migrations)

### 切換資料庫後的遷移步驟

#### 步驟 1: 更新配置
編輯 `appsettings.Development.json`，設定目標資料庫提供者。

#### 步驟 2: 刪除舊遷移 (可選)
如果要從頭開始：
```bash
Remove-Item -Path .\Migrations\* -Recurse -Force
```

#### 步驟 3: 建立新遷移
```bash
dotnet ef migrations add InitialCreate
```

#### 步驟 4: 應用遷移
```bash
dotnet ef database update
```

### 保留多個資料庫的遷移

如果需要同時支援多個資料庫，可以為每個資料庫建立專屬的遷移：

```bash
# SQLite
dotnet ef migrations add InitialCreate_SQLite --context ApplicationDbContext --output-dir Migrations/SQLite

# SQL Server
dotnet ef migrations add InitialCreate_SqlServer --context ApplicationDbContext --output-dir Migrations/SqlServer

# PostgreSQL
dotnet ef migrations add InitialCreate_Postgres --context ApplicationDbContext --output-dir Migrations/Postgres
```

## Hangfire 支援

目前 Hangfire 配置為使用與主資料庫相同的連線。

**注意：** 當前實作僅針對 SQLite 進行了完整配置。若要使用 SQL Server 或 PostgreSQL 作為 Hangfire 儲存，需要安裝額外套件：

```bash
# SQL Server
dotnet add package Hangfire.SqlServer

# PostgreSQL
dotnet add package Hangfire.PostgreSql
```

並修改 `Program.cs` 中的 Hangfire 配置。

## 驗證配置

啟動應用程式時，會在控制台輸出當前使用的資料庫提供者：

```
Using Database Provider: Sqlite
SQLite Database Path: C:\...\bin\Debug\net9.0\SocialEngineeringDB.db
```

或

```
Using Database Provider: SqlServer
Using SQL Server database
```

## Git 版本控制

### .gitignore 配置

專案已配置 `.gitignore` 排除資料庫檔案：

```gitignore
# Entity Framework Core SQLite databases
*.db
*.db-shm
*.db-wal
*.sqlite
*.sqlite3
```

### 如果資料庫已被追蹤

如果 `.db` 檔案已經被 Git 追蹤，執行：

```bash
git rm --cached SocialEngineeringDB.db
git commit -m "Remove database file from version control"
```

## 環境特定配置

### 開發環境 (`appsettings.Development.json`)
```json
{
  "DatabaseProvider": "Sqlite",
  "ConnectionStrings": {
    "SqliteConnection": "Data Source=SocialEngineeringDB.db"
  }
}
```

### 生產環境 (`appsettings.json`)
```json
{
  "DatabaseProvider": "SqlServer",
  "ConnectionStrings": {
    "SqlServerConnection": "Server=prod-server;Database=SocialEngineeringDB;User Id=app_user;Password=SecurePassword"
  }
}
```

### 測試環境 (`appsettings.Testing.json`)
```json
{
  "DatabaseProvider": "Sqlite",
  "ConnectionStrings": {
    "SqliteConnection": "Data Source=:memory:"
  }
}
```

## 故障排除

### 問題: 找不到資料庫提供者
**解決方案:** 確保 `appsettings.json` 中有 `DatabaseProvider` 設定。

### 問題: SQL Server 連線失敗
**檢查項目:**
- SQL Server 服務是否正在運行
- 連線字串是否正確
- 防火牆是否允許連接
- 使用者權限是否足夠

### 問題: PostgreSQL 連線失敗
**檢查項目:**
- PostgreSQL 是否正在運行 (`sudo systemctl status postgresql`)
- `pg_hba.conf` 是否允許連接
- 資料庫是否已建立

### 問題: 遷移失敗
**解決方案:**
1. 確保目標資料庫伺服器正在運行
2. 驗證連線字串
3. 檢查資料庫使用者權限

## 效能建議

### SQLite
- 使用 WAL 模式 (預設已啟用)
- 定期執行 `VACUUM` 清理
- 避免大量並發寫入

### SQL Server
- 建立適當的索引
- 使用連線池 (預設已啟用)
- 定期更新統計資訊

### PostgreSQL
- 調整 `shared_buffers` 和 `work_mem`
- 啟用查詢計劃快取
- 使用連線池 (pgBouncer)

## 相關檔案

- `Program.cs` - 資料庫提供者切換邏輯
- `appsettings.json` - 生產環境配置
- `appsettings.Development.json` - 開發環境配置
- `.gitignore` - Git 忽略規則
- `Data/ApplicationDbContext.cs` - EF Core DbContext

## 更多資源

- [Entity Framework Core 文件](https://docs.microsoft.com/ef/core/)
- [SQL Server 連線字串](https://www.connectionstrings.com/sql-server/)
- [PostgreSQL 連線字串](https://www.connectionstrings.com/postgresql/)
- [Hangfire 文件](https://docs.hangfire.io/)
