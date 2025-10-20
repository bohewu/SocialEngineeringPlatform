using Hangfire.PostgreSql; // 支援 Hangfire PostgreSQL
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SocialEngineeringPlatform.Web.Data;
using SocialEngineeringPlatform.Web.Models.Configuration;
using SocialEngineeringPlatform.Web.Models.Core;
using SocialEngineeringPlatform.Web.Services;
using SocialEngineeringPlatform.Web.Services.Interfaces; // 引用 ApplicationUser
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.MemoryStorage;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);


// *** 新增：註冊資料保護服務 ***
// AddDataProtection 可以選擇性地配置金鑰儲存位置 (預設儲存在使用者設定檔或機器金鑰)
// 對於生產環境，建議配置持久化的金鑰儲存 (例如 Azure Blob Storage, Redis, File System)
// builder.Services.AddDataProtection()
//    .PersistKeysToFileSystem(new DirectoryInfo(@"c:\keys")) // 範例：儲存到檔案系統
//    .ProtectKeysWithDpapi(); // 範例：使用 DPAPI 加密金鑰
builder.Services.AddDataProtection(); // 使用預設設定 (適用於開發或單一伺服器)

// *** 新增：註冊設定服務 ***
builder.Services.AddScoped<ISettingsService, DatabaseSettingsService>();

// --- 多資料庫支援配置 ---
var dbProvider = builder.Configuration.GetValue<string>("DatabaseProvider") ?? "Sqlite";
Console.WriteLine($"Using Database Provider: {dbProvider}");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    switch (dbProvider.ToLower())
    {
        case "sqlite":
            // SQLite: 動態計算絕對路徑，確保資料庫與執行檔在同一目錄
            var sqliteConnStr = builder.Configuration.GetConnectionString("SqliteConnection")
                                ?? builder.Configuration.GetConnectionString("DefaultConnection")
                                ?? throw new InvalidOperationException("SQLite connection string not found.");

            if (sqliteConnStr.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
            {
                var dataSourcePrefix = "Data Source=";
                var dbFileName = sqliteConnStr.Substring(dataSourcePrefix.Length).Trim();

                if (!Path.IsPathRooted(dbFileName))
                {
                    var appBasePath = AppContext.BaseDirectory;
                    var databasePath = Path.Combine(appBasePath, dbFileName);
                    sqliteConnStr = $"{dataSourcePrefix}{databasePath}";
                    Console.WriteLine($"SQLite Database Path: {databasePath}");
                }
            }

            options.UseSqlite(sqliteConnStr);
            break;

        case "sqlserver":
        case "mssql":
            // SQL Server
            var sqlServerConnStr = builder.Configuration.GetConnectionString("SqlServerConnection")
                                   ?? throw new InvalidOperationException("SQL Server connection string not found.");
            options.UseSqlServer(sqlServerConnStr);
            Console.WriteLine("Using SQL Server database");
            break;

        case "postgres":
        case "postgresql":
            // PostgreSQL
            var postgresConnStr = builder.Configuration.GetConnectionString("PostgresConnection")
                                  ?? throw new InvalidOperationException("PostgreSQL connection string not found.");
            options.UseNpgsql(postgresConnStr);
            Console.WriteLine("Using PostgreSQL database");
            break;

        default:
            throw new Exception(
                $"Unsupported database provider: {dbProvider}. Supported providers: Sqlite, SqlServer, Postgres");
    }
});

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
builder.Services.AddScoped<IMailService, SmtpMailService>();
builder.Services.AddScoped<ICampaignExecutionService, CampaignExecutionService>();

// Minimal Health Checks
builder.Services
    .AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy());

// --- 修改 Identity 設定以使用 ApplicationUser ---
builder.Services
    .AddDefaultIdentity<
        ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true) // <--- 指定使用 ApplicationUser
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Add services to the container. (保留原本的 AddRazorPages 等設定)
builder.Services.AddRazorPages()
    .AddRazorPagesOptions(options =>
    {
        // 預設要求根目錄 ("/") 下的所有頁面都需要授權 (登入)
        options.Conventions.AuthorizeFolder("/");

        // 但允許匿名存取 Identity 相關的頁面 (登入、註冊等)
        options.Conventions.AllowAnonymousToFolder("/Identity");

        // 允許匿名存取錯誤頁面
        options.Conventions.AllowAnonymousToPage("/Error");

        // 允許匿名存取隱私權頁面 (如果有的話)
        options.Conventions.AllowAnonymousToPage("/Privacy");

        // 注意：這樣設定後，首頁 (Index.cshtml) 也會需要登入才能存取
        // 如果希望首頁匿名可見，可以取消註解下一行：
        // options.Conventions.AllowAnonymousToPage("/Index");

        // // *** 新增：限制 /Admin/Roles 資料夾需要 Administrator 角色 ***
        // options.Conventions.AuthorizeFolder("/Admin/Roles", ApplicationDbContext.RoleAdmin);
        // // *** 新增：限制 /Admin/Users 資料夾需要 Administrator 角色 (預先加入) ***
        // options.Conventions.AuthorizeFolder("/Admin/Users", ApplicationDbContext.RoleAdmin);
        // // *** 新增：限制 /Settings 資料夾需要 Administrator 角色 (預先加入) ***
        // options.Conventions.AuthorizeFolder("/Settings", ApplicationDbContext.RoleAdmin);

        options.Conventions.AllowAnonymousToPage("/TrackClick");
        options.Conventions.AllowAnonymousToPage("/TrackLanding");
        options.Conventions.AllowAnonymousToPage("/TrackOpen");
        options.Conventions.AllowAnonymousToPage("/TrackSubmit");
    });

// *** 新增：設定 Hangfire (根據資料庫提供者) ***
builder.Services.AddHangfire(configuration =>
{
    configuration
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings();

    switch (dbProvider.ToLower())
    {
        case "sqlite":
            // SQLite 模式下使用 MemoryStorage
            configuration.UseMemoryStorage();
            Console.WriteLine("Hangfire using MemoryStorage (SQLite mode)");
            break;
        case "sqlserver":
        case "mssql":
            var sqlServerConnStr = builder.Configuration.GetConnectionString("SqlServerConnection")
                                   ?? throw new InvalidOperationException("Hangfire SQL Server connection string not found.");
            configuration.UseSqlServerStorage(sqlServerConnStr);
            Console.WriteLine("Hangfire using SQL Server Storage");
            break;
        case "postgres":
        case "postgresql":
            var postgresConnStr = builder.Configuration.GetConnectionString("PostgresConnection")
                                  ?? throw new InvalidOperationException("Hangfire PostgreSQL connection string not found.");
            configuration.UsePostgreSqlStorage(options => { options.UseNpgsqlConnection(postgresConnStr); });
            Console.WriteLine("Hangfire using PostgreSQL Storage");
            break;
        default:
            throw new Exception($"Unsupported Hangfire provider: {dbProvider}");
    }
});

// *** 新增：啟動 Hangfire 伺服器 ***
// AddHangfireServer 會在背景啟動處理工作的伺服器
builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = Environment.ProcessorCount * 2; // 可以根據伺服器資源調整 Worker 數量
    options.Queues = ["default"]; // 可以定義不同的佇列
});
// *** Hangfire 設定結束 ***


var app = builder.Build();

// --- *** 在應用程式啟動時執行資料庫遷移與植入 *** ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        // --- 執行資料植入 ---
        await DbInitializer.InitializeAsync(services); // 呼叫植入方法
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>(); // Program 本身的 Logger
        logger.LogError(ex, "An error occurred seeding the DB.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// --- 確保 UseAuthentication 在 UseAuthorization 之前 ---
app.UseAuthentication(); // 加入這行 (如果 Identity 範本沒有自動加的話)
app.UseAuthorization();

// *** 新增：加入 Hangfire Dashboard 中介軟體 ***
// 預設儀表板路徑是 /hangfire
// 可以加入授權過濾器來保護儀表板
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new HangfireDashboardAuthorizationFilter()],
    // IsReadOnlyFunc = _ => true
});
// *** Hangfire Dashboard 設定結束 ***

app.MapRazorPages();
app.MapControllers(); // 如果您有 API Controller，也需要 Map

// Health check endpoint
app.MapHealthChecks("/health");

app.Run();


public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        // 檢查使用者是否已登入且具有特定角色
        return httpContext.User.Identity?.IsAuthenticated == true &&
               httpContext.User.IsInRole(ApplicationDbContext.RoleAdmin); // 只允許管理員訪問
    }
}