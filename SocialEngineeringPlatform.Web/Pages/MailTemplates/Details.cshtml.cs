using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore; // 需要引用 EntityFrameworkCore
using SocialEngineeringPlatform.Web.Data;
using SocialEngineeringPlatform.Web.Models.Core;

// 加入授權

namespace SocialEngineeringPlatform.Web.Pages.MailTemplates
{
    // [Authorize] // 如果需要，加上授權限制
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public MailTemplate MailTemplate { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // 查詢範本資料，並同時載入關聯的 Category 和 CreatedByUser
            var mailtemplate = await _context.MailTemplates
                .Include(m => m.Category)
                .Include(m => m.CreatedByUser)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (mailtemplate == null)
            {
                return NotFound();
            }
            else
            {
                MailTemplate = mailtemplate;
            }
            return Page();
        }
    }
}