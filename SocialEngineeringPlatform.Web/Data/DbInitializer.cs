using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SocialEngineeringPlatform.Web.Models.Core;
using static SocialEngineeringPlatform.Web.Common.LoggingHelper;

namespace SocialEngineeringPlatform.Web.Data
{
    public static class DbInitializer
    {
        // 資料植入的主要方法
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            // 取得所需的服務
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>(); // 取得設定
            // *** 修改：取得 ILoggerFactory 來建立 Logger ***
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("DbInitializer"); // 使用字串指定 Logger 分類

            // --- 選擇性：自動套用掛起的遷移 ---
            // logger.LogInformation("Checking/Applying database migrations...");
            // await context.Database.MigrateAsync(); // 取消註解此行可在啟動時自動更新資料庫
            // logger.LogInformation("Database migration check/apply completed.");


            // --- 植入角色 (確保存在) ---
            logger.LogInformation("Seeding roles...");
            await EnsureRoleExistsAsync(roleManager, ApplicationDbContext.RoleAdmin, logger);
            await EnsureRoleExistsAsync(roleManager, ApplicationDbContext.RoleCampaignManager, logger);
            // await EnsureRoleExistsAsync(roleManager, ApplicationDbContext.RoleReportViewer, logger);
            logger.LogInformation("Role seeding completed.");


            // --- 植入預設管理員帳號 ---
            logger.LogInformation("Seeding default admin user...");
            await CreateDefaultAdminUserAsync(userManager, configuration, logger);
            logger.LogInformation("Default admin user seeding completed.");


            // --- 植入其他資料 (例如: 預設分類) ---
            logger.LogInformation("Seeding default categories...");
            await SeedDefaultCategoriesAsync(context, logger);
            logger.LogInformation("Default category seeding completed.");
            // await SeedDefaultTemplatesAsync(context);
        }

        // 確保角色存在的方法
        private static async Task EnsureRoleExistsAsync(RoleManager<IdentityRole> roleManager, string roleName,
            ILogger logger)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(roleName));
                if (result.Succeeded)
                {
                    logger.LogInformation($"Role '{roleName}' created successfully.");
                }
                else
                {
                    logger.LogError(
                        $"Error creating role '{roleName}'. Error count: {result.Errors.Count()}");
                }
            }
            else
            {
                logger.LogInformation($"Role '{roleName}' already exists.");
            }
        }

        // 建立預設管理員帳號的方法
        private static async Task CreateDefaultAdminUserAsync(UserManager<ApplicationUser> userManager,
            IConfiguration configuration, ILogger logger)
        {
            // 從設定檔讀取預設管理員資訊
            var adminEmail = configuration["AppSettings:AdminUser:Email"] ?? "admin@example.com";
            var adminPassword = configuration["AppSettings:AdminUser:Password"];
            
            if(adminPassword == null)
            {
                logger.LogError("Admin password is not set in the configuration.");
                return;
            }

            // 檢查管理員帳號是否已存在
            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true // 直接設定為已確認 (開發方便)
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);

                if (result.Succeeded)
                {
                    // 將使用者加入 Administrator 角色
                    var addToRoleResult = await userManager.AddToRoleAsync(adminUser, ApplicationDbContext.RoleAdmin);
                    if (addToRoleResult.Succeeded)
                    {
                        logger.LogInformation(
                            $"Default admin user '{MaskEmail(adminEmail)}' created successfully and added to role '{ApplicationDbContext.RoleAdmin}'.");
                    }
                    else
                    {
                        logger.LogError(
                            $"Default admin user '{MaskEmail(adminEmail)}' created but failed to add to role '{ApplicationDbContext.RoleAdmin}'. Error count: {addToRoleResult.Errors.Count()}");
                    }
                }
                else
                {
                    logger.LogError(
                        $"Error creating default admin user '{MaskEmail(adminEmail)}'. Error count: {result.Errors.Count()}");
                }
            }
            else
            {
                logger.LogInformation($"Default admin user '{MaskEmail(adminEmail)}' already exists.");
            }
        }

        // 植入預設分類的方法 (範例)
        private static async Task SeedDefaultCategoriesAsync(ApplicationDbContext context, ILogger logger)
        {
            const string defaultCategoryName = "預設分類";
            if (!await context.MailTemplateCategories.AnyAsync(c => c.Name == defaultCategoryName))
            {
                context.MailTemplateCategories.Add(new MailTemplateCategory
                {
                    Name = defaultCategoryName,
                    Description = "系統自動建立的預設分類"
                });
                try
                {
                    await context.SaveChangesAsync();
                    logger.LogInformation($"Default mail template category '{defaultCategoryName}' seeded.");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Error seeding default mail template category '{defaultCategoryName}'.");
                }
            }
            else
            {
                logger.LogInformation($"Default mail template category '{defaultCategoryName}' already exists.");
            }
        }
    }
}