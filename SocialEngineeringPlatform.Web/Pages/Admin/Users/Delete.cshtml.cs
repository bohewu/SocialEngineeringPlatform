using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SocialEngineeringPlatform.Web.Data;
using SocialEngineeringPlatform.Web.Models.Core;

namespace SocialEngineeringPlatform.Web.Pages.Admin.Users
{
    [Authorize(Roles = ApplicationDbContext.RoleAdmin)]
    public class DeleteModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<DeleteModel> _logger;

        public DeleteModel(UserManager<ApplicationUser> userManager, ILogger<DeleteModel> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public ApplicationUser UserToDelete { get; set; } = default!;
        
        // *** 新增：標記是否能刪除 ***
        public bool CanDelete { get; set; } = true;

        // OnGet 用於顯示要刪除的使用者資訊
        public async Task<IActionResult> OnGetAsync(string? id) // User ID 是 string
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            UserToDelete = await _userManager.FindByIdAsync(id);

            if (UserToDelete == null)
            {
                return NotFound($"找不到 ID 為 '{id}' 的使用者。");
            }
            
            // *** 新增：檢查是否刪除自己 ***
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (UserToDelete.Id == currentUserId)
            {
                CanDelete = false; // 不允許刪除自己
            }
            
            return Page();
        }

        // OnPost 用於處理刪除確認
        public async Task<IActionResult> OnPostAsync(string? id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }
            
            // *** 新增：檢查是否刪除自己 ***
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (id == currentUserId)
            {
                TempData["ErrorMessage"] = "您無法刪除自己的帳號。";
                return RedirectToPage("./Index");
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = $"找不到 ID 為 '{id}' 的使用者，可能已被刪除。";
                return RedirectToPage("./Index");
            }

            // *** 重要：刪除使用者前，應考慮相關資料的處理 ***
            // 例如：此使用者建立的 Campaign, MailTemplate 等是否要一併刪除？
            //       或者將這些資料的 CreatedByUserId 設為 null 或指向某個預設帳號？
            //       目前 Identity 的級聯刪除設定可能只處理 Identity 內部的關聯表。
            //       直接刪除可能導致外鍵約束錯誤。
            //
            // 建議：與其直接刪除，不如先實作「鎖定帳號」功能 (LockoutEnabled / LockoutEnd)
            //       或加入一個 IsActive 欄位來禁用帳號。
            //
            // 此處暫時保留刪除邏輯，但加上警告。

            _logger.LogWarning("準備刪除使用者：{UserName} (ID: {UserId})。請確保已處理相關資料！", user.UserName, user.Id);

            var result = await _userManager.DeleteAsync(user);

            if (result.Succeeded)
            {
                _logger.LogInformation("成功刪除使用者：{UserName} (ID: {UserId})", user.UserName, user.Id);
                TempData["SuccessMessage"] = $"使用者 '{user.UserName}' 已成功刪除！";
            }
            else
            {
                 _logger.LogError("刪除使用者 '{UserName}' (ID: {UserId}) 時發生錯誤。", user.UserName, user.Id);
                 TempData["ErrorMessage"] = $"刪除使用者 '{user.UserName}' 時發生錯誤。";
                 foreach (var error in result.Errors) { TempData["ErrorMessage"] += $" {error.Description}"; }
                 // 刪除失敗返回 Index
            }

            return RedirectToPage("./Index");
        }
    }
}