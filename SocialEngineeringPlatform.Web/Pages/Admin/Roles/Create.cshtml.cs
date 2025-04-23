using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SocialEngineeringPlatform.Web.Data;

namespace SocialEngineeringPlatform.Web.Pages.Admin.Roles
{
    [Authorize(Roles = ApplicationDbContext.RoleAdmin)]
    public class CreateModel : PageModel
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<CreateModel> _logger;

        public CreateModel(RoleManager<IdentityRole> roleManager, ILogger<CreateModel> logger)
        {
            _roleManager = roleManager;
            _logger = logger;
        }

        // 使用 InputModel 模式來接收表單輸入
        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        public class InputModel
        {
            [Required(ErrorMessage = "角色名稱為必填欄位。")]
            [StringLength(100, ErrorMessage = "{0} 長度必須介於 {2} 到 {1} 個字元之間。", MinimumLength = 2)]
            [Display(Name = "角色名稱")]
            public string RoleName { get; set; } = "";
        }

        // GET 請求只顯示頁面
        public void OnGet()
        {
        }

        // POST 請求處理建立角色
        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                // 檢查角色名稱是否已存在
                if (await _roleManager.RoleExistsAsync(Input.RoleName))
                {
                    ModelState.AddModelError(string.Empty, $"角色名稱 '{Input.RoleName}' 已經存在。");
                    return Page();
                }

                // 建立新的 IdentityRole 物件
                var newRole = new IdentityRole(Input.RoleName);
                // 使用 RoleManager 建立角色
                var result = await _roleManager.CreateAsync(newRole);

                if (result.Succeeded)
                {
                    _logger.LogInformation("成功建立新角色：{RoleName}", Input.RoleName);
                    TempData["SuccessMessage"] = $"角色 '{Input.RoleName}' 已成功建立！";
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