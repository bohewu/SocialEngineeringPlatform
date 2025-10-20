using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SocialEngineeringPlatform.Web.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: true),
                    SecurityStamp = table.Column<string>(type: "TEXT", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: true),
                    PhoneNumber = table.Column<string>(type: "TEXT", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MailTemplateCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MailTemplateCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SmtpSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Host = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    Port = table.Column<int>(type: "INTEGER", nullable: false),
                    EnableSsl = table.Column<bool>(type: "INTEGER", nullable: false),
                    Username = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    EncryptedPassword = table.Column<string>(type: "TEXT", nullable: true),
                    FromAddress = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    FromDisplayName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SmtpSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TargetGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreateTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdateTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TargetGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RoleId = table.Column<string>(type: "TEXT", nullable: false),
                    ClaimType = table.Column<string>(type: "TEXT", nullable: true),
                    ClaimValue = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    ClaimType = table.Column<string>(type: "TEXT", nullable: true),
                    ClaimValue = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    ProviderKey = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "TEXT", nullable: true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    RoleId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    LoginProvider = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LandingPageTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    HtmlContent = table.Column<string>(type: "TEXT", nullable: false),
                    CollectFieldsConfig = table.Column<string>(type: "TEXT", nullable: true),
                    CreateTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdateTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedByUserId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LandingPageTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LandingPageTemplates_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MailTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Subject = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Body = table.Column<string>(type: "TEXT", nullable: false),
                    Language = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    CategoryId = table.Column<int>(type: "INTEGER", nullable: true),
                    AttachmentPath = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    CustomFromAddress = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    CustomFromDisplayName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    CreateTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdateTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedByUserId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MailTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MailTemplates_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MailTemplates_MailTemplateCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "MailTemplateCategories",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TargetUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    GroupId = table.Column<int>(type: "INTEGER", nullable: true),
                    CustomField1 = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    CustomField2 = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdateTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TargetUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TargetUsers_TargetGroups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "TargetGroups",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Campaigns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    MailTemplateId = table.Column<int>(type: "INTEGER", nullable: false),
                    LandingPageTemplateId = table.Column<int>(type: "INTEGER", nullable: true),
                    TargetGroupId = table.Column<int>(type: "INTEGER", nullable: true),
                    ScheduledSendTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ActualStartTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EndTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    IsAutoSend = table.Column<bool>(type: "INTEGER", nullable: false),
                    SendBatchDelaySeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    TrackOpens = table.Column<bool>(type: "INTEGER", nullable: false),
                    JobId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CreateTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdateTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedByUserId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Campaigns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Campaigns_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Campaigns_LandingPageTemplates_LandingPageTemplateId",
                        column: x => x.LandingPageTemplateId,
                        principalTable: "LandingPageTemplates",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Campaigns_MailTemplates_MailTemplateId",
                        column: x => x.MailTemplateId,
                        principalTable: "MailTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Campaigns_TargetGroups_TargetGroupId",
                        column: x => x.TargetGroupId,
                        principalTable: "TargetGroups",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CampaignTargets",
                columns: table => new
                {
                    CampaignId = table.Column<int>(type: "INTEGER", nullable: false),
                    TargetUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    SendStatus = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    SendTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampaignTargets", x => new { x.CampaignId, x.TargetUserId });
                    table.ForeignKey(
                        name: "FK_CampaignTargets_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CampaignTargets_TargetUsers_TargetUserId",
                        column: x => x.TargetUserId,
                        principalTable: "TargetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MailSendLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CampaignId = table.Column<int>(type: "INTEGER", nullable: false),
                    TargetUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    SendTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    SmtpServerUsed = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MailSendLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MailSendLogs_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MailSendLogs_TargetUsers_TargetUserId",
                        column: x => x.TargetUserId,
                        principalTable: "TargetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrackingEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CampaignId = table.Column<int>(type: "INTEGER", nullable: false),
                    TargetUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    MailTemplateId = table.Column<int>(type: "INTEGER", nullable: true),
                    LandingPageTemplateId = table.Column<int>(type: "INTEGER", nullable: true),
                    EventTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    EventType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    EventDetails = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackingEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrackingEvents_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TrackingEvents_LandingPageTemplates_LandingPageTemplateId",
                        column: x => x.LandingPageTemplateId,
                        principalTable: "LandingPageTemplates",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TrackingEvents_MailTemplates_MailTemplateId",
                        column: x => x.MailTemplateId,
                        principalTable: "MailTemplates",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TrackingEvents_TargetUsers_TargetUserId",
                        column: x => x.TargetUserId,
                        principalTable: "TargetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "2ec0da4a-9c5a-4e4b-9969-0d94e8e18320", null, "Administrator", "ADMINISTRATOR" },
                    { "a80015e12-3913-453c-980f-268ec79224c4", null, "CampaignManager", "CAMPAIGNMANAGER" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_CreatedByUserId",
                table: "Campaigns",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_LandingPageTemplateId",
                table: "Campaigns",
                column: "LandingPageTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_MailTemplateId",
                table: "Campaigns",
                column: "MailTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_TargetGroupId",
                table: "Campaigns",
                column: "TargetGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_CampaignTargets_TargetUserId",
                table: "CampaignTargets",
                column: "TargetUserId");

            migrationBuilder.CreateIndex(
                name: "IX_LandingPageTemplates_CreatedByUserId",
                table: "LandingPageTemplates",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MailSendLogs_CampaignId",
                table: "MailSendLogs",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_MailSendLogs_TargetUserId",
                table: "MailSendLogs",
                column: "TargetUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MailTemplates_CategoryId",
                table: "MailTemplates",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_MailTemplates_CreatedByUserId",
                table: "MailTemplates",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TargetGroups_Name",
                table: "TargetGroups",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TargetUsers_Email",
                table: "TargetUsers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TargetUsers_GroupId",
                table: "TargetUsers",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_TrackingEvents_CampaignId",
                table: "TrackingEvents",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_TrackingEvents_LandingPageTemplateId",
                table: "TrackingEvents",
                column: "LandingPageTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_TrackingEvents_MailTemplateId",
                table: "TrackingEvents",
                column: "MailTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_TrackingEvents_TargetUserId",
                table: "TrackingEvents",
                column: "TargetUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "CampaignTargets");

            migrationBuilder.DropTable(
                name: "MailSendLogs");

            migrationBuilder.DropTable(
                name: "SmtpSettings");

            migrationBuilder.DropTable(
                name: "TrackingEvents");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "Campaigns");

            migrationBuilder.DropTable(
                name: "TargetUsers");

            migrationBuilder.DropTable(
                name: "LandingPageTemplates");

            migrationBuilder.DropTable(
                name: "MailTemplates");

            migrationBuilder.DropTable(
                name: "TargetGroups");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "MailTemplateCategories");
        }
    }
}
