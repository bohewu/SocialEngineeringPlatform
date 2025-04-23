using Microsoft.AspNetCore.Authorization; // For Authorize attribute/policy
using Microsoft.AspNetCore.Identity; // For RoleManager
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore; // For ToListAsync
using SocialEngineeringPlatform.Web.Data; // For Role Names constant

namespace SocialEngineeringPlatform.Web.Pages.Admin.Roles
{
    // *** 使用 Authorize 屬性限制只有 Administrator 角色可以訪問 ***
    // (如果已在 Program.cs 中設定 AuthorizeFolder，則此處可省略，但加上更明確)
    [Authorize(Roles = ApplicationDbContext.RoleAdmin)]
    public class IndexModel : PageModel
    {
        private readonly RoleManager<IdentityRole> _roleManager; // 注入 RoleManager

        public IndexModel(RoleManager<IdentityRole> roleManager)
        {
            _roleManager = roleManager;
        }

        // 用於在頁面顯示的角色列表
        public IList<IdentityRole> Roles { get;set; } = default!;

        // OnGetAsync 載入所有角色
        public async Task OnGetAsync()
        {
            // 從 RoleManager 取得所有角色，並依照名稱排序
            Roles = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();
        }
    }
}