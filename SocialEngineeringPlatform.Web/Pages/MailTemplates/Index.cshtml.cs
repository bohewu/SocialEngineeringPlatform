using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore; // 需要引用 EntityFrameworkCore
using SocialEngineeringPlatform.Web.Data;
using SocialEngineeringPlatform.Web.Models.Core;

// 加入授權

namespace SocialEngineeringPlatform.Web.Pages.MailTemplates
{
    // [Authorize] // 如果需要，加上授權限制
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // 屬性名稱建議用複數或加上 List/Collection 後綴較清晰
        public IList<MailTemplate> MailTemplateList { get;set; } = default!;

        public async Task OnGetAsync()
        {
            // 查詢郵件範本，並同時載入關聯的分類和建立者資訊
            MailTemplateList = await _context.MailTemplates
                .Include(m => m.Category) // 載入分類
                .Include(m => m.CreatedByUser) // 載入建立者
                .ToListAsync();
        }
    }
}