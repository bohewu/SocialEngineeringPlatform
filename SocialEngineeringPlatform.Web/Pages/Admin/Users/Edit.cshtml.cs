using System.ComponentModel.DataAnnotations;
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
    public class EditModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<EditModel> _logger;

        public EditModel(UserManager<ApplicationUser> userManager, ILogger<EditModel> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        // 使用 InputModel 模式
        [BindProperty] public InputModel Input { get; set; } = new InputModel();

        // *** 新增：標記是否正在編輯目前登入的使用者 ***
        public bool IsEditingSelf { get; set; } = false;

        public class InputModel
        {
            [Required] public string Id { get; set; } = "";

            [Display(Name = "使用者名稱")] public string? UserName { get; set; }

            [Required(ErrorMessage = "電子郵件為必填。")]
            [EmailAddress(ErrorMessage = "電子郵件格式不正確。")]
            [Display(Name = "電子郵件")]
            public string Email { get; set; } = "";

            [Phone(ErrorMessage = "電話號碼格式不正確。")]
            [Display(Name = "電話號碼")]
            public string? PhoneNumber { get; set; }

            [Display(Name = "帳號已鎖定")] public bool IsLockedOut { get; set; }

            [Display(Name = "鎖定結束時間 (UTC)")]
            [DataType(DataType.DateTime)]
            public DateTimeOffset? LockoutEnd { get; set; }
        }

        // OnGet 用於載入使用者資料
        public async Task<IActionResult> OnGetAsync(string? id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound($"找不到 ID 為 '{id}' 的使用者。");
            }

            // *** 新增：檢查是否編輯自己 ***
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier); // 取得目前登入者的 ID
            IsEditingSelf = (user.Id == currentUserId);

            // 將使用者資料填入 InputModel
            Input = new InputModel
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email ?? "", // Email 通常不為 null
                PhoneNumber = user.PhoneNumber,
                IsLockedOut = await _userManager.IsLockedOutAsync(user),
                LockoutEnd = user.LockoutEnd
            };

            return Page();
        }

        // OnPost 用於處理編輯表單提交
        public async Task<IActionResult> OnPostAsync()
        {
            // *** 新增：在 OnPost 也需要知道是否在編輯自己 ***
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            IsEditingSelf = (Input.Id == currentUserId);

            // *** 修改：如果正在編輯自己，則不處理鎖定相關欄位 ***
            if (IsEditingSelf)
            {
                ModelState.Remove("Input.IsLockedOut");
                ModelState.Remove("Input.LockoutEnd");
            }

            if (!ModelState.IsValid)
            {
                return Page(); // 返回頁面顯示驗證錯誤
            }

            var user = await _userManager.FindByIdAsync(Input.Id);
            if (user == null)
            {
                return NotFound($"找不到 ID 為 '{Input.Id}' 的使用者。");
            }

            bool changed = false; // 追蹤是否有變更

            // 更新 UserName (如果與目前不同)
            var currentUserName = await _userManager.GetUserNameAsync(user);
            if (Input.UserName != currentUserName)
            {
                // 檢查新的 UserName 是否已被使用 (如果需要唯一性)
                // var existingUser = await _userManager.FindByNameAsync(Input.UserName);
                // if (existingUser != null && existingUser.Id != user.Id) { ... add error ... }
                var setUserNameResult = await _userManager.SetUserNameAsync(user, Input.UserName);
                if (!setUserNameResult.Succeeded)
                {
                    /* AddErrors(setUserNameResult); return Page(); */
                }

                changed = true;
            }

            // 更新 Email (如果與目前不同)
            var currentEmail = await _userManager.GetEmailAsync(user);
            if (Input.Email != currentEmail)
            {
                // 檢查新的 Email 是否已被使用
                var existingUser = await _userManager.FindByEmailAsync(Input.Email);
                if (existingUser != null && existingUser.Id != user.Id)
                {
                    ModelState.AddModelError("Input.Email", $"電子郵件 '{Input.Email}' 已被其他帳號使用。");
                    return Page();
                }

                var setEmailResult = await _userManager.SetEmailAsync(user, Input.Email);
                if (!setEmailResult.Succeeded)
                {
                    /* AddErrors(setEmailResult); return Page(); */
                }

                // 注意：更改 Email 後，EmailConfirmed 可能會變為 false，需要重新驗證
                // 可以考慮是否要強制設回 true 或維持 Identity 的行為
                // await _userManager.UpdateNormalizedEmailAsync(user); // 確保 NormalizedEmail 更新
                changed = true;
            }

            // 更新 PhoneNumber (如果與目前不同)
            var currentPhoneNumber = await _userManager.GetPhoneNumberAsync(user);
            if (Input.PhoneNumber != currentPhoneNumber)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    /* AddErrors(setPhoneResult); return Page(); */
                }

                // 更改電話後，PhoneNumberConfirmed 可能會變為 false
                changed = true;
            }

            // *** 修改：只有在不是編輯自己時才更新鎖定狀態 ***
            if (!IsEditingSelf)
            {
                // 更新鎖定狀態
                var currentLockoutEnabled = user.LockoutEnabled; // 直接讀取屬性
                var currentLockoutEnd = user.LockoutEnd;
                bool isCurrentlyLockedOut = await _userManager.IsLockedOutAsync(user);

                if (Input.IsLockedOut && !isCurrentlyLockedOut) // 如果要鎖定且目前未鎖定
                {
                    // 設定一個非常遠的未來時間來永久鎖定，或使用 Input.LockoutEnd
                    DateTimeOffset lockoutUntil =
                        Input.LockoutEnd.HasValue && Input.LockoutEnd.Value > DateTimeOffset.UtcNow
                            ? Input.LockoutEnd.Value
                            : DateTimeOffset.MaxValue; // 永久鎖定
                    var setLockoutResult = await _userManager.SetLockoutEndDateAsync(user, lockoutUntil);
                    if (!setLockoutResult.Succeeded)
                    {
                        /* AddErrors(setLockoutResult); return Page(); */
                    }

                    // 確保 LockoutEnabled 為 true (通常 SetLockoutEndDateAsync 會處理)
                    if (!currentLockoutEnabled) await _userManager.SetLockoutEnabledAsync(user, true);
                    _logger.LogInformation("已鎖定使用者 {UserId} 直到 {LockoutEnd}", user.Id, lockoutUntil);
                    changed = true;
                }
                else if (!Input.IsLockedOut && isCurrentlyLockedOut) // 如果要解鎖且目前已鎖定
                {
                    // 將 LockoutEnd 設為 null 或過去時間即可解鎖
                    var setLockoutResult = await _userManager.SetLockoutEndDateAsync(user, null);
                    if (!setLockoutResult.Succeeded)
                    {
                        /* AddErrors(setLockoutResult); return Page(); */
                    }

                    _logger.LogInformation("已解鎖使用者 {UserId}", user.Id);
                    changed = true;
                }
                // 如果 IsLockedOut 為 true 且 isCurrentlyLockedOut 為 true，檢查 LockoutEnd 是否需要更新
                else if (Input.IsLockedOut && isCurrentlyLockedOut && Input.LockoutEnd != currentLockoutEnd)
                {
                    DateTimeOffset lockoutUntil =
                        Input.LockoutEnd.HasValue && Input.LockoutEnd.Value > DateTimeOffset.UtcNow
                            ? Input.LockoutEnd.Value
                            : DateTimeOffset.MaxValue;
                    var setLockoutResult = await _userManager.SetLockoutEndDateAsync(user, lockoutUntil);
                    if (!setLockoutResult.Succeeded)
                    {
                        /* AddErrors(setLockoutResult); return Page(); */
                    }

                    _logger.LogInformation("已更新使用者 {UserId} 的鎖定結束時間為 {LockoutEnd}", user.Id, lockoutUntil);
                    changed = true;
                }
            } 
            else if(Input.IsLockedOut || Input.LockoutEnd.HasValue) // 如果是編輯自己但嘗試修改鎖定狀態，給予提示
            {
                _logger.LogWarning("使用者 {CurrentUserId} 嘗試修改自己的鎖定狀態，操作已被忽略。", currentUserId);
                TempData["WarningMessage"] = "您無法鎖定自己的帳號。";
            }
            
            if (changed)
            {
                // 如果有任何變更，則更新使用者記錄
                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    _logger.LogError("更新使用者 {UserId} 時發生錯誤: {Errors}", user.Id,
                        string.Join(", ", updateResult.Errors.Select(e => e.Description)));
                    foreach (var error in updateResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }

                    return Page();
                }

                TempData["SuccessMessage"] = $"使用者 '{user.UserName}' 的資料已成功更新。";
            }
            else
            {
                TempData["InfoMessage"] = "沒有偵測到任何變更。";
            }


            return RedirectToPage("./Index"); // 成功後返回使用者列表頁
        }

        // 輔助方法將 IdentityResult 錯誤加入 ModelState
        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
    }
}