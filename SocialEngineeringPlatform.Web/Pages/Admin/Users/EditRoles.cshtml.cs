using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore; // For ToListAsync on roles
using SocialEngineeringPlatform.Web.Data;
using SocialEngineeringPlatform.Web.Models.Core;

namespace SocialEngineeringPlatform.Web.Pages.Admin.Users
{
    [Authorize(Roles = ApplicationDbContext.RoleAdmin)]
    public class EditRolesModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<EditRolesModel> _logger;

        public EditRolesModel(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<EditRolesModel> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        // 從路由接收 UserId
        [BindProperty(SupportsGet = true)]
        public string UserId { get; set; } = ""; // Identity User ID is string

        // 顯示使用者名稱
        public string UserName { get; set; } = "";

        // 用於顯示和提交角色選擇的 ViewModel
        public List<RoleSelectionViewModel> Roles { get; set; } = new List<RoleSelectionViewModel>();
        
        // *** 新增：標記是否正在編輯目前登入的使用者 ***
        public bool IsEditingSelf { get; set; } = false;

        // 內部 ViewModel
        public class RoleSelectionViewModel
        {
            public string RoleId { get; set; } = "";
            public string RoleName { get; set; } = "";
            public bool IsSelected { get; set; }
        }

        // OnGet 用於載入使用者及其角色資訊
        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.FindByIdAsync(UserId);
            if (user == null)
            {
                return NotFound($"找不到 ID 為 '{UserId}' 的使用者。");
            }
            UserName = user.UserName ?? user.Email ?? "未知使用者"; // 顯示使用者名稱或 Email

            // *** 新增：檢查是否編輯自己 ***
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            IsEditingSelf = (user.Id == currentUserId);
            
            await ReloadRolesForPage(user);
            return Page();
        }

        // OnPost 用於處理角色指派的變更
        public async Task<IActionResult> OnPostAsync([FromForm] List<string> selectedRoles) // 從表單接收選取的角色名稱列表
        {
            var user = await _userManager.FindByIdAsync(UserId);
            if (user == null)
            {
                return NotFound($"找不到 ID 為 '{UserId}' 的使用者。");
            }

            // *** 新增：在 OnPost 也需要知道是否在編輯自己 ***
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            IsEditingSelf = (user.Id == currentUserId);
            
            
            // 取得使用者目前的角色
            var currentUserRoles = await _userManager.GetRolesAsync(user);

            // *** 修改：如果是編輯自己，檢查是否嘗試移除 Admin 角色 ***
            if (IsEditingSelf && currentUserRoles.Contains(ApplicationDbContext.RoleAdmin) && !selectedRoles.Contains(ApplicationDbContext.RoleAdmin))
            {
                ModelState.AddModelError(string.Empty, "無法移除您自己的 Administrator 角色。");
                await ReloadRolesForPage(user); // 重新載入以顯示錯誤
                return Page();
            }
            
            // 計算需要新增的角色 (在 selectedRoles 中，但不在 currentUserRoles 中)
            var rolesToAdd = selectedRoles.Except(currentUserRoles).ToList();
            if (rolesToAdd.Any())
            {
                var addResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
                if (!addResult.Succeeded)
                {
                    _logger.LogError("為使用者 {UserId} 新增角色時發生錯誤: {Errors}", UserId, string.Join(", ", addResult.Errors.Select(e => e.Description)));
                    // 將錯誤加入 ModelState 以便顯示
                    foreach (var error in addResult.Errors) { ModelState.AddModelError(string.Empty, error.Description); }
                    // 出錯時需要重新載入角色列表以顯示頁面
                    await ReloadRolesForPage(user);
                    return Page();
                }
                 _logger.LogInformation("成功為使用者 {UserId} 新增角色: {Roles}", UserId, string.Join(", ", rolesToAdd));
            }

            // 計算需要移除的角色 (在 currentUserRoles 中，但不在 selectedRoles 中)
            var rolesToRemove = currentUserRoles.Except(selectedRoles).ToList();
            if (rolesToRemove.Any())
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
                 if (!removeResult.Succeeded)
                {
                    _logger.LogError("為使用者 {UserId} 移除角色時發生錯誤: {Errors}", UserId, string.Join(", ", removeResult.Errors.Select(e => e.Description)));
                    foreach (var error in removeResult.Errors) { ModelState.AddModelError(string.Empty, error.Description); }
                    await ReloadRolesForPage(user);
                    return Page();
                }
                 _logger.LogInformation("成功為使用者 {UserId} 移除角色: {Roles}", UserId, string.Join(", ", rolesToRemove));
            }

            TempData["SuccessMessage"] = $"使用者 '{user.UserName}' 的角色已成功更新。";
            return RedirectToPage("./Index"); // 成功後返回使用者列表頁
        }

        // 輔助方法：重新載入角色列表 (用於 POST 失敗時)
        private async Task ReloadRolesForPage(ApplicationUser user)
        {
            UserName = user.UserName ?? user.Email ?? "未知使用者";
            var allRoles = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();
            var userRoles = await _userManager.GetRolesAsync(user);
            Roles = allRoles.Select(role => new RoleSelectionViewModel
            {
                RoleId = role.Id,
                RoleName = role.Name ?? "未知角色名稱",
                IsSelected = userRoles.Contains(role.Name ?? "")
            }).ToList();
        }
    }
}
