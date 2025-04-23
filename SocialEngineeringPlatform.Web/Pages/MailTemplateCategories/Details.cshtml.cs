using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SocialEngineeringPlatform.Web.Data;
using SocialEngineeringPlatform.Web.Models.Core;

// 加入授權

namespace SocialEngineeringPlatform.Web.Pages.MailTemplateCategories
{
    // [Authorize] // 如果需要，加上授權限制
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public MailTemplateCategory MailTemplateCategory { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound(); // 如果沒有提供 id，返回 404
            }

            // 根據 id 查詢分類資料
            var mailtemplatecategory = await _context.MailTemplateCategories.FirstOrDefaultAsync(m => m.Id == id);
            if (mailtemplatecategory == null)
            {
                return NotFound(); // 如果找不到資料，返回 404
            }
            else
            {
                MailTemplateCategory = mailtemplatecategory; // 將查詢到的資料賦值給頁面模型
            }
            return Page(); // 返回頁面
        }
    }
}