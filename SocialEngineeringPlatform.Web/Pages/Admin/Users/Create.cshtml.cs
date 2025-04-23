using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SocialEngineeringPlatform.Web.Data;
using SocialEngineeringPlatform.Web.Models.Core;

namespace SocialEngineeringPlatform.Web.Pages.Admin.Users
{
    [Authorize(Roles = ApplicationDbContext.RoleAdmin)]
    public class CreateModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<CreateModel> _logger;
        // 可以選擇性注入 SignInManager 如果需要在建立後自動登入 (通常管理員建立帳號不需要)
        // private readonly SignInManager<ApplicationUser> _signInManager;

        public CreateModel(
            UserManager<ApplicationUser> userManager,
            ILogger<CreateModel> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        // InputModel for the create user form
        public class InputModel
        {
            [Required(ErrorMessage = "電子郵件為必填。")]
            [EmailAddress(ErrorMessage = "電子郵件格式不正確。")]
            [Display(Name = "電子郵件")]
            public string Email { get; set; } = "";

            // UserName 可以等於 Email 或自訂
            [Required(ErrorMessage = "使用者名稱為必填。")]
            [Display(Name = "使用者名稱")]
            public string UserName { get; set; } = "";

            [Required(ErrorMessage = "密碼為必填。")]
            [StringLength(100, ErrorMessage = "{0} 長度必須至少 {2} 個字元，最多 {1} 個字元。", MinimumLength = 6)] // Identity 預設密碼長度要求
            [DataType(DataType.Password)]
            [Display(Name = "密碼")]
            public string Password { get; set; } = "";

            [DataType(DataType.Password)]
            [Display(Name = "確認密碼")]
            [Compare("Password", ErrorMessage = "密碼與確認密碼不符。")]
            public string ConfirmPassword { get; set; } = "";
        }

        // GET 請求只顯示頁面
        public void OnGet()
        {
            // 可以預設 UserName 等於 Email (如果需要)
            // Input.UserName = Input.Email;
        }

        // POST 請求處理建立使用者
        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                // 檢查 Email 是否已存在
                var existingUserByEmail = await _userManager.FindByEmailAsync(Input.Email);
                if (existingUserByEmail != null)
                {
                    ModelState.AddModelError("Input.Email", $"電子郵件 '{Input.Email}' 已被註冊。");
                    return Page();
                }
                // 檢查 UserName 是否已存在
                var existingUserByName = await _userManager.FindByNameAsync(Input.UserName);
                 if (existingUserByName != null)
                {
                    ModelState.AddModelError("Input.UserName", $"使用者名稱 '{Input.UserName}' 已被使用。");
                    return Page();
                }


                // 建立新的 ApplicationUser 物件
                var user = new ApplicationUser
                {
                    UserName = Input.UserName,
                    Email = Input.Email,
                    EmailConfirmed = true // 管理員建立的帳號預設為已驗證
                    // PhoneNumber = ..., // 可以增加電話號碼欄位
                };

                // 使用 UserManager 建立使用者 (包含密碼雜湊)
                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("管理員成功建立新使用者 '{UserName}'。", Input.UserName);

                    // 可以選擇性地將新使用者加入預設角色，例如 CampaignManager
                    // var addToRoleResult = await _userManager.AddToRoleAsync(user, ApplicationDbContext.RoleCampaignManager);
                    // if (!addToRoleResult.Succeeded) { _logger.LogWarning("無法將新使用者加入預設角色。"); /* Handle error */ }

                    TempData["SuccessMessage"] = $"使用者 '{Input.UserName}' 已成功建立！";
                    return RedirectToPage("./Index"); // 成功後導向列表頁
                }
                else
                {
                    // 如果建立失敗，將錯誤訊息加入 ModelState
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
            }

            // 如果執行到這裡，表示有錯誤，重新顯示表單
            return Page();
        }
    }
}