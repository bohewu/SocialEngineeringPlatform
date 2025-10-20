# SQLite + 多資料庫支援實作總結

## 📋 已完成的工作

### ✅ 步驟 E: Git 版本控制配置

**完成項目：**
1. 建立 `.gitignore` 檔案
2. 排除所有資料庫檔案 (`*.db`, `*.sqlite`, `*.sqlite3`)
3. 排除建置產物 (`bin/`, `obj/`, `publish/`)
4. 排除使用者特定檔案

**檔案位置：**
- `c:\repos\SocialEngineeringPlatform\SocialEngineeringPlatform.Web\.gitignore`

**使用說明：**
如果資料庫檔案已被 Git 追蹤，執行：
```bash
git rm --cached SocialEngineeringDB.db
git commit -m "Remove database from version control"
```

---

### ✅ 步驟 F: 多資料庫支援實作

**完成項目：**
1. 安裝必要的 NuGet 套件
   - ✅ `Npgsql.EntityFrameworkCore.PostgreSQL` (v9.0.4)
   - ✅ `Microsoft.EntityFrameworkCore.SqlServer` (已存在)
   - ✅ `Microsoft.EntityFrameworkCore.Sqlite` (已存在)

2. 更新配置檔案
   - ✅ `appsettings.json` - 新增 `DatabaseProvider` 和多個連線字串
   - ✅ `appsettings.Development.json` - 同步更新

3. 修改 `Program.cs`
   - ✅ 實作資料庫提供者切換邏輯 (switch 語句)
   - ✅ SQLite 路徑自動解析 (AppContext.BaseDirectory)
   - ✅ SQL Server 支援
   - ✅ PostgreSQL 支援
   - ✅ Hangfire 連線字串動態配置

---

## 🎯 功能特性

### 1. 彈性的資料庫切換

**透過配置檔切換：**
```json
{
  "DatabaseProvider": "Sqlite"  // 選項: "Sqlite", "SqlServer", "Postgres"
}
```

**支援的資料庫：**
| 資料庫 | 關鍵字 | 適用場景 |
|--------|--------|----------|
| SQLite | `Sqlite` | 開發、測試、小型部署 |
| SQL Server | `SqlServer` 或 `mssql` | 企業環境、高並發 |
| PostgreSQL | `Postgres` 或 `postgresql` | 開源方案、高效能 |

### 2. 自動路徑解析 (SQLite)

SQLite 資料庫檔案會自動放置在執行檔目錄：
- 開發：`bin/Debug/net9.0/SocialEngineeringDB.db`
- 發佈：`publish/SocialEngineeringDB.db`

### 3. 環境特定配置

不同環境可使用不同的資料庫：
```
開發 (appsettings.Development.json) → SQLite
測試 (appsettings.Testing.json) → SQLite (in-memory)
生產 (appsettings.json) → SQL Server
```

---

## 📂 檔案變更清單

### 新建檔案
1. `.gitignore` - Git 版本控制配置
2. `SQLITE_DEPLOYMENT_GUIDE.md` - SQLite 部署指南
3. `MULTI_DATABASE_GUIDE.md` - 多資料庫配置指南

### 修改檔案
1. `Program.cs` - 多資料庫支援邏輯
2. `appsettings.json` - 新增 DatabaseProvider 和連線字串
3. `appsettings.Development.json` - 開發環境配置
4. `SocialEngineeringPlatform.Web.csproj` - 新增 PostgreSQL 套件

---

## 🚀 使用範例

### 範例 1: 開發環境使用 SQLite

`appsettings.Development.json`:
```json
{
  "DatabaseProvider": "Sqlite",
  "ConnectionStrings": {
    "SqliteConnection": "Data Source=SocialEngineeringDB.db"
  }
}
```

啟動應用程式：
```bash
dotnet run
```

輸出：
```
Using Database Provider: Sqlite
SQLite Database Path: C:\...\bin\Debug\net9.0\SocialEngineeringDB.db
```

### 範例 2: 生產環境使用 SQL Server

`appsettings.json`:
```json
{
  "DatabaseProvider": "SqlServer",
  "ConnectionStrings": {
    "SqlServerConnection": "Server=prod-server;Database=SocialEngineeringDB;User Id=app_user;Password=SecurePass"
  }
}
```

部署後：
```
Using Database Provider: SqlServer
Using SQL Server database
```

### 範例 3: 使用環境變數覆蓋

```bash
# PowerShell
$env:DatabaseProvider="Postgres"
dotnet run
```

---

## 📊 架構圖

```
appsettings.json
    ↓
[DatabaseProvider: "Sqlite"]
    ↓
Program.cs (switch dbProvider)
    ↓
┌─────────┬──────────────┬────────────┐
│ SQLite  │  SQL Server  │ PostgreSQL │
└─────────┴──────────────┴────────────┘
    ↓
ApplicationDbContext (Entity Framework Core)
    ↓
[Your Application]
```

---

## ✅ 驗證步驟

### 1. 驗證 SQLite (預設)
```bash
dotnet build
dotnet run
# 檢查輸出: "Using Database Provider: Sqlite"
```

### 2. 驗證 SQL Server
修改 `appsettings.Development.json`:
```json
"DatabaseProvider": "SqlServer"
```
```bash
dotnet ef database update
dotnet run
# 檢查輸出: "Using SQL Server database"
```

### 3. 驗證 PostgreSQL
修改 `appsettings.Development.json`:
```json
"DatabaseProvider": "Postgres"
```
```bash
dotnet ef database update
dotnet run
# 檢查輸出: "Using PostgreSQL database"
```

---

## 🔧 技術實作細節

### Program.cs 核心邏輯

```csharp
var dbProvider = builder.Configuration.GetValue<string>("DatabaseProvider") ?? "Sqlite";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    switch (dbProvider.ToLower())
    {
        case "sqlite":
            // SQLite 路徑自動解析
            var sqliteConnStr = GetSQLiteConnectionString();
            options.UseSqlite(sqliteConnStr);
            break;

        case "sqlserver":
        case "mssql":
            var sqlServerConnStr = GetSQLServerConnectionString();
            options.UseSqlServer(sqlServerConnStr);
            break;

        case "postgres":
        case "postgresql":
            var postgresConnStr = GetPostgresConnectionString();
            options.UseNpgsql(postgresConnStr);
            break;
    }
});
```

### 連線字串優先順序

1. 環境變數 (最高優先)
2. `appsettings.{Environment}.json`
3. `appsettings.json` (預設)

---

## 📝 後續建議

### 建議 1: 資料庫遷移策略
- 為不同資料庫保留獨立的遷移資料夾
- 使用 `--output-dir` 參數分離遷移

### 建議 2: Hangfire 完整支援
目前 Hangfire 僅完整支援 SQLite。若要使用其他資料庫：

```bash
# SQL Server
dotnet add package Hangfire.SqlServer

# PostgreSQL  
dotnet add package Hangfire.PostgreSql
```

並修改 `Program.cs` 中的 Hangfire 配置。

### 建議 3: 連線池配置
為 SQL Server 和 PostgreSQL 配置適當的連線池大小：

```json
"SqlServerConnection": "Server=...;Max Pool Size=100;Min Pool Size=10"
"PostgresConnection": "Host=...;Pooling=true;Maximum Pool Size=100"
```

### 建議 4: 健康檢查
新增資料庫健康檢查端點：

```csharp
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>();

app.MapHealthChecks("/health");
```

---

## 🎓 學習資源

- **Entity Framework Core**: https://docs.microsoft.com/ef/core/
- **SQLite 最佳實踐**: https://www.sqlite.org/bestpractice.html
- **SQL Server 效能調校**: https://docs.microsoft.com/sql/
- **PostgreSQL 文件**: https://www.postgresql.org/docs/

---

## 📞 支援

如有問題，請參考：
1. `SQLITE_DEPLOYMENT_GUIDE.md` - SQLite 專用指南
2. `MULTI_DATABASE_GUIDE.md` - 多資料庫配置詳細指南

---

**實作日期**: 2025-10-20  
**專案**: SocialEngineeringPlatform.Web  
**版本**: 1.0.0  
**狀態**: ✅ 完成並已驗證
