# SQLite 資料庫部署指南

## 概述
本專案已配置為使用 SQLite 資料庫，資料庫檔案會始終與應用程式執行檔 (.exe/.dll) 保持在同一目錄下。

## 配置說明

### 1. 連線字串配置 (appsettings.json)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=SocialEngineeringDB.db"
  }
}
```
- 只需指定檔案名稱，程式會自動計算絕對路徑

### 2. 專案檔配置 (.csproj)
```xml
<ItemGroup>
  <Content Include="SocialEngineeringDB.db" Condition="Exists('SocialEngineeringDB.db')">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
</ItemGroup>
```
- 確保資料庫檔案在建置/發佈時自動複製到輸出目錄
- 使用 `Condition` 避免檔案不存在時的錯誤

### 3. 動態路徑解析 (Program.cs)
```csharp
// 動態計算 SQLite 資料庫的絕對路徑
if (connectionString.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
{
    var dataSourcePrefix = "Data Source=";
    var dbFileName = connectionString.Substring(dataSourcePrefix.Length).Trim();
    
    if (!Path.IsPathRooted(dbFileName))
    {
        // AppContext.BaseDirectory 指向應用程式執行目錄
        var appBasePath = AppContext.BaseDirectory;
        var databasePath = Path.Combine(appBasePath, dbFileName);
        connectionString = $"{dataSourcePrefix}{databasePath}";
        
        Console.WriteLine($"SQLite Database Path: {databasePath}");
    }
}
```

## 部署流程

### 開發環境
1. **建置專案**
   ```bash
   dotnet build
   ```
   - 資料庫檔案位於：`bin/Debug/net9.0/SocialEngineeringDB.db`

2. **執行遷移** (首次或資料庫結構變更時)
   ```bash
   dotnet ef database update
   ```
   - 資料庫會自動建立在 `bin/Debug/net9.0/` 目錄下

3. **複製資料庫到專案根目錄** (供發佈使用)
   ```bash
   Copy-Item ".\bin\Debug\net9.0\SocialEngineeringDB.db" ".\SocialEngineeringDB.db" -Force
   ```

### 生產環境部署

1. **發佈應用程式**
   ```bash
   dotnet publish -c Release -o ./publish
   ```

2. **驗證發佈結果**
   發佈目錄 (`./publish/`) 應包含：
   - `SocialEngineeringPlatform.Web.dll` (應用程式執行檔)
   - `SocialEngineeringDB.db` (資料庫檔案)
   - 其他依賴檔案

3. **部署到伺服器**
   - 將整個 `publish/` 目錄複製到伺服器
   - 資料庫檔案會自動與執行檔在同一目錄
   - 應用程式執行時會自動找到資料庫

## 資料庫位置

| 環境 | 資料庫位置 |
|------|-----------|
| 開發 (Debug) | `bin/Debug/net9.0/SocialEngineeringDB.db` |
| 發佈 (Release) | `bin/Release/net9.0/publish/SocialEngineeringDB.db` |
| 生產伺服器 | `[應用程式目錄]/SocialEngineeringDB.db` |

## 優點

✅ **可攜性**：資料庫檔案隨執行檔移動  
✅ **簡單性**：無需外部資料庫伺服器  
✅ **自動化**：建置/發佈時自動處理  
✅ **一致性**：開發與生產環境行為一致  

## 注意事項

⚠️ **備份**：定期備份 `SocialEngineeringDB.db` 檔案  
⚠️ **權限**：確保應用程式有讀寫資料庫檔案的權限  
⚠️ **並發**：SQLite 支援有限的並發寫入，適合中小型應用  
⚠️ **遷移**：更新資料庫結構後需重新發佈或手動執行遷移  

## 常見問題

### Q: 如何更改資料庫位置？
A: 在 `appsettings.json` 中使用絕對路徑：
```json
"DefaultConnection": "Data Source=C:\\Data\\MyDatabase.db"
```

### Q: 如何查看當前資料庫路徑？
A: 應用程式啟動時會在控制台輸出：
```
SQLite Database Path: C:\...\SocialEngineeringDB.db
```

### Q: 發佈後找不到資料庫？
A: 確保：
1. 專案根目錄有 `SocialEngineeringDB.db` 檔案
2. `.csproj` 包含複製設定
3. 重新執行 `dotnet publish`

## 資料庫管理工具推薦

- **DB Browser for SQLite**: https://sqlitebrowser.org/
- **DBeaver**: https://dbeaver.io/
- **Visual Studio Code** + SQLite 擴充功能
