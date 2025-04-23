using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SocialEngineeringPlatform.Web.Data;
using SocialEngineeringPlatform.Web.Models.Core;

// 加入授權

namespace SocialEngineeringPlatform.Web.Pages.MailTemplateCategories
{
    // [Authorize] // 如果需要，加上授權限制
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<MailTemplateCategory> MailTemplateCategory { get;set; } = default!;

        public async Task OnGetAsync()
        {
            // 從資料庫取得所有分類資料
            MailTemplateCategory = await _context.MailTemplateCategories.ToListAsync();
        }
    }
}