using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SocialEngineeringPlatform.Web.Models.Core; // 引用您的模型命名空間

namespace SocialEngineeringPlatform.Web.Data
{
    // 將繼承目標從 IdentityDbContext 改為 IdentityDbContext<ApplicationUser>
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser> // 使用自訂的 ApplicationUser
    {
        public const string RoleAdmin = "Administrator";
        public const string RoleCampaignManager = "CampaignManager";
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // --- 加入您自訂模型的 DbSet ---
        public DbSet<TargetUser> TargetUsers { get; set; }
        public DbSet<TargetGroup> TargetGroups { get; set; }
        public DbSet<MailTemplate> MailTemplates { get; set; }
        public DbSet<LandingPageTemplate> LandingPageTemplates { get; set; }
        public DbSet<Campaign> Campaigns { get; set; }
        public DbSet<CampaignTarget> CampaignTargets { get; set; }
        public DbSet<TrackingEvent> TrackingEvents { get; set; }
        public DbSet<MailSendLog> MailSendLogs { get; set; }
        public DbSet<MailTemplateCategory> MailTemplateCategories { get; set; } // 加入分類表 DbSet

        // *** 新增：SMTP 設定的 DbSet ***
        public DbSet<DbSmtpSetting> SmtpSettings { get; set; }
        
        // --- 使用 Fluent API 設定模型關係 (OnModelCreating) ---
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder); // 非常重要！保留 Identity 的設定

            // --- 設定複合主鍵 (Composite Primary Key) ---
            builder.Entity<CampaignTarget>()
                .HasKey(ct => new { ct.CampaignId, ct.TargetUserId }); // CampaignTargets 的複合主鍵

            // --- 設定唯一約束 (Unique Constraint) ---
            builder.Entity<TargetUser>()
                .HasIndex(u => u.Email)
                .IsUnique(); // Email 欄位唯一

            builder.Entity<TargetGroup>()
                .HasIndex(g => g.Name)
                .IsUnique(); // 群組名稱唯一

            // --- *** 設定 Enum 轉換與欄位屬性 *** ---
            builder.Entity<Campaign>()
                .Property(c => c.Status)
                .HasConversion<string>() // 將 Enum 存為字串
                .HasMaxLength(50);       // 設定資料庫欄位最大長度

            builder.Entity<TrackingEvent>()
                .Property(te => te.EventType)
                .HasConversion<string>() // 將 Enum 存為字串
                .HasMaxLength(50);       // 設定資料庫欄位最大長度

            builder.Entity<CampaignTarget>()
                .Property(ct => ct.SendStatus)
                .HasConversion<string>() // 將 Enum 存為字串
                .HasMaxLength(50);       // 設定資料庫欄位最大長度

            builder.Entity<MailSendLog>()
                .Property(ml => ml.Status)
                .HasConversion<string>() // 將 Enum 存為字串
                .HasMaxLength(50);       // 設定資料庫欄位最大長度


            // --- 設定級聯刪除行為 (Cascade Delete Behavior) ---
            builder.Entity<TrackingEvent>()
                .HasOne(te => te.Campaign)
                .WithMany(c => c.TrackingEvents)
                .HasForeignKey(te => te.CampaignId)
                .OnDelete(DeleteBehavior.Restrict);

             builder.Entity<MailSendLog>()
                .HasOne(ml => ml.Campaign)
                .WithMany(c => c.MailSendLogs)
                .HasForeignKey(ml => ml.CampaignId)
                .OnDelete(DeleteBehavior.Restrict);

             builder.Entity<CampaignTarget>()
                .HasOne(ct => ct.Campaign)
                .WithMany(c => c.CampaignTargets)
                .HasForeignKey(ct => ct.CampaignId)
                .OnDelete(DeleteBehavior.Cascade);

             builder.Entity<CampaignTarget>()
                .HasOne(ct => ct.TargetUser)
                .WithMany(tu => tu.CampaignTargets)
                .HasForeignKey(ct => ct.TargetUserId)
                .OnDelete(DeleteBehavior.Cascade);


            // --- 設定 ApplicationUser 與其他模型的關係 ---
            builder.Entity<ApplicationUser>()
                .HasMany(u => u.CreatedCampaigns)
                .WithOne(c => c.CreatedByUser)
                .HasForeignKey(c => c.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ApplicationUser>()
                .HasMany(u => u.CreatedMailTemplates)
                .WithOne(mt => mt.CreatedByUser)
                .HasForeignKey(mt => mt.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ApplicationUser>()
                .HasMany(u => u.CreatedLandingPageTemplates)
                .WithOne(lt => lt.CreatedByUser)
                .HasForeignKey(lt => lt.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);


            // --- 設定其他關係和欄位屬性 ---
            builder.Entity<TrackingEvent>().Property(e => e.Id).ValueGeneratedOnAdd();
            builder.Entity<MailSendLog>().Property(e => e.Id).ValueGeneratedOnAdd();

            // 設定時間預設值 (使用 SQL Server 的 GETUTCDATE() 函數)
            builder.Entity<TargetUser>().Property(u => u.CreateTime).HasDefaultValueSql("GETUTCDATE()");
            builder.Entity<TargetUser>().Property(u => u.UpdateTime).HasDefaultValueSql("GETUTCDATE()");
            builder.Entity<TargetGroup>().Property(g => g.CreateTime).HasDefaultValueSql("GETUTCDATE()");
            builder.Entity<TargetGroup>().Property(g => g.UpdateTime).HasDefaultValueSql("GETUTCDATE()");
            builder.Entity<MailTemplate>().Property(mt => mt.CreateTime).HasDefaultValueSql("GETUTCDATE()");
            builder.Entity<MailTemplate>().Property(mt => mt.UpdateTime).HasDefaultValueSql("GETUTCDATE()");
            builder.Entity<LandingPageTemplate>().Property(lt => lt.CreateTime).HasDefaultValueSql("GETUTCDATE()");
            builder.Entity<LandingPageTemplate>().Property(lt => lt.UpdateTime).HasDefaultValueSql("GETUTCDATE()");
            builder.Entity<Campaign>().Property(c => c.CreateTime).HasDefaultValueSql("GETUTCDATE()");
            builder.Entity<Campaign>().Property(c => c.UpdateTime).HasDefaultValueSql("GETUTCDATE()");
            builder.Entity<TrackingEvent>().Property(te => te.EventTime).HasDefaultValueSql("GETUTCDATE()");
            builder.Entity<MailSendLog>().Property(ml => ml.SendTime).HasDefaultValueSql("GETUTCDATE()");
            
            // *** 新增：明確設定 IdentityRole 的主鍵 (嘗試解決索引問題) ***
            // 雖然 base 方法已處理，但明確指定可能有助於解析
            builder.Entity<IdentityRole>(b =>
            {
                b.HasKey(r => r.Id); // 明確指定 Id 為主鍵
                // 可以考慮明確指定欄位類型，但先嘗試僅指定主鍵
                // b.Property(r => r.Id).HasMaxLength(450); // EF Core 預設
            });
            // *** 新增結束 ***
            
            // ... 其他模型的 Fluent API 設定 ...
            
            // --- *** 植入初始角色資料 (Seed Roles) *** ---
            // --- *** 植入初始角色資料 (Seed Roles) *** ---
            // *** 修改：使用固定的 Guid 字串，而非 Guid.NewGuid() ***
            // 您可以使用任何 Guid 產生器產生一次，然後固定使用這些值
            const string ADMIN_ROLE_ID = "2ec0da4a-9c5a-4e4b-9969-0d94e8e18320";
            const string MANAGER_ROLE_ID = "a80015e12-3913-453c-980f-268ec79224c4";
            // const string VIEWER_ROLE_ID = "a3b3c3d3-e3f3-7777-8888-999999999999";
            // string viewerRoleId = Guid.NewGuid().ToString();

            builder.Entity<IdentityRole>().HasData(
                new IdentityRole
                {
                    Id = ADMIN_ROLE_ID,
                    Name = RoleAdmin,
                    NormalizedName = RoleAdmin.ToUpperInvariant() // 標準化名稱
                },
                new IdentityRole
                {
                    Id = MANAGER_ROLE_ID,
                    Name = RoleCampaignManager,
                    NormalizedName = RoleCampaignManager.ToUpperInvariant()
                }
                // 可視需求加入更多角色
            );
        }
    }
}