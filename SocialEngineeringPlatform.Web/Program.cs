using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SocialEngineeringPlatform.Web.Data;
using SocialEngineeringPlatform.Web.Models.Configuration;
using SocialEngineeringPlatform.Web.Models.Core;
using SocialEngineeringPlatform.Web.Services;
using SocialEngineeringPlatform.Web.Services.Interfaces; // 引用 ApplicationUser
using Hangfire;
using Hangfire.Dashboard; // *** 加入 Hangfire using ***
using Hangfire.SqlServer; // *** 加入 Hangfire SQL Server using ***

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

// --- 修改資料庫設定 ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                       throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// 將 AddDbContext 中的 UseSqlite 改為 UseSqlServer
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString)); // <--- 改成 UseSqlServer

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
builder.Services.AddScoped<IMailService, SmtpMailService>();
builder.Services.AddScoped<ICampaignExecutionService, CampaignExecutionService>();

// --- 修改 Identity 設定以使用 ApplicationUser ---
builder.Services
    .AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true) // <--- 指定使用 ApplicationUser
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

// *** 新增：設定 Hangfire ***
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180) // 設定相容性等級
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(connectionString, new SqlServerStorageOptions // 使用 SQL Server 儲存
    {
        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
        QueuePollInterval = TimeSpan.Zero, // 使用 SQL Server 的通知機制，而不是輪詢
        UseRecommendedIsolationLevel = true,
        DisableGlobalLocks = true // 建議在高流量環境下啟用
    }));

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