using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore; // 需要引用 EntityFrameworkCore
using SocialEngineeringPlatform.Web.Data;
using SocialEngineeringPlatform.Web.Models.Core;

namespace SocialEngineeringPlatform.Web.Pages.LandingPageTemplates
{
    // [Authorize]
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public LandingPageTemplate LandingPageTemplate { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // 查詢範本資料，並載入建立者資訊
            var landingpagetemplate = await _context.LandingPageTemplates
                .Include(l => l.CreatedByUser) // 載入建立者
                .AsNoTracking() // 因為只是顯示，不需要追蹤
                .FirstOrDefaultAsync(m => m.Id == id);

            if (landingpagetemplate == null)
            {
                return NotFound();
            }
            else
            {
                LandingPageTemplate = landingpagetemplate;
            }
            return Page();
        }
    }
}