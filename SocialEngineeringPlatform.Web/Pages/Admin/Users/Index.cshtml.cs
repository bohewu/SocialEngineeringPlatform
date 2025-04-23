using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore; // For ToListAsync
using SocialEngineeringPlatform.Web.Data; // For RoleAdmin constant
using SocialEngineeringPlatform.Web.Models.Core; // For ApplicationUser

namespace SocialEngineeringPlatform.Web.Pages.Admin.Users
{
    // 限制只有 Administrator 可以訪問
    [Authorize(Roles = ApplicationDbContext.RoleAdmin)]
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager; // 注入 UserManager

        public IndexModel(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        // 用於在頁面顯示的使用者列表
        public IList<ApplicationUser> Users { get;set; } = default!;

        // OnGetAsync 載入所有使用者
        public async Task OnGetAsync()
        {
            // 從 UserManager 取得所有使用者，並依照使用者名稱排序
            Users = await _userManager.Users.OrderBy(u => u.UserName).ToListAsync();
            // 注意：這裡沒有載入每個使用者的角色資訊，可以在需要時（例如 Edit 頁面）再載入
        }
    }
}