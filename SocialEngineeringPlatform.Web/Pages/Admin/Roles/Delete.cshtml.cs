using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SocialEngineeringPlatform.Web.Data;

namespace SocialEngineeringPlatform.Web.Pages.Admin.Roles
{
    [Authorize(Roles = ApplicationDbContext.RoleAdmin)]
    public class DeleteModel : PageModel
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<Models.Core.ApplicationUser> _userManager; // 需要 UserManager 檢查是否有使用者屬於此角色
        private readonly ILogger<DeleteModel> _logger;

        public DeleteModel(RoleManager<IdentityRole> roleManager, UserManager<Models.Core.ApplicationUser> userManager, ILogger<DeleteModel> logger)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public IdentityRole RoleToDelete { get; set; } = default!;

        public bool CanDelete { get; set; } = true;
        public string? DeletionWarningMessage { get; set; }

        // OnGet 用於顯示要刪除的角色資訊
        public async Task<IActionResult> OnGetAsync(string? id) // Role ID 是 string
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            RoleToDelete = await _roleManager.FindByIdAsync(id);

            if (RoleToDelete == null)
            {
                return NotFound($"找不到 ID 為 '{id}' 的角色。");
            }

            // 檢查是否有使用者屬於此角色
            var usersInRole = await _userManager.GetUsersInRoleAsync(RoleToDelete.Name!); // Name 不應為 null
            if (usersInRole.Any())
            {
                CanDelete = false;
                DeletionWarningMessage = $"無法刪除角色 '{RoleToDelete.Name}'，因為目前有 {usersInRole.Count} 位使用者屬於此角色。請先將這些使用者移出此角色。";
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

            // 再次查找角色
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
            {
                TempData["ErrorMessage"] = $"找不到 ID 為 '{id}' 的角色，可能已被刪除。";
                return RedirectToPage("./Index");
            }

            // 再次檢查是否有使用者屬於此角色 (防止在 GET 和 POST 之間狀態改變)
            var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);
            if (usersInRole.Any())
            {
                 TempData["ErrorMessage"] = $"無法刪除角色 '{role.Name}'，因為仍有使用者屬於此角色。";
                 return RedirectToPage("./Index"); // 或者導向回 Delete 頁面顯示錯誤
            }

            // 執行刪除
            var result = await _roleManager.DeleteAsync(role);

            if (result.Succeeded)
            {
                _logger.LogInformation("成功刪除角色：{RoleName} (ID: {RoleId})", role.Name, role.Id);
                TempData["SuccessMessage"] = $"角色 '{role.Name}' 已成功刪除！";
            }
            else
            {
                 _logger.LogError("刪除角色 '{RoleName}' (ID: {RoleId}) 時發生錯誤。", role.Name, role.Id);
                 TempData["ErrorMessage"] = $"刪除角色 '{role.Name}' 時發生錯誤。";
                 // 可以將錯誤訊息加入 TempData 或 ModelState
                 // foreach (var error in result.Errors) { TempData["ErrorMessage"] += $" {error.Description}"; }
            }

            return RedirectToPage("./Index");
        }
    }
}