using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore; // 需要引用 EntityFrameworkCore
using SocialEngineeringPlatform.Web.Data;
using SocialEngineeringPlatform.Web.Models.Core;

// 加入授權

namespace SocialEngineeringPlatform.Web.Pages.TargetUsers
{
    // [Authorize] // 如果需要，加上授權限制
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public TargetUser TargetUser { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // 查詢使用者資料，並使用 Include 載入關聯的 TargetGroup 資料
            var targetuser = await _context.TargetUsers
                .Include(t => t.Group) // 載入關聯的 Group
                .FirstOrDefaultAsync(m => m.Id == id);

            if (targetuser == null)
            {
                return NotFound();
            }
            else
            {
                TargetUser = targetuser;
            }
            return Page();
        }
    }
}