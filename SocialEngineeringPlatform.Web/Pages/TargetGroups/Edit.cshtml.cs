using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SocialEngineeringPlatform.Web.Data;
using SocialEngineeringPlatform.Web.Models.Core;

// 加入授權

namespace SocialEngineeringPlatform.Web.Pages.TargetGroups
{
    // [Authorize] // 如果尚未在 Program.cs 中設定 AuthorizeFolder("/TargetGroups")，則需要加上此屬性
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // 使用 [BindProperty] 將表單送過來的資料以及 GET 請求的 id 對應到這個屬性
        [BindProperty]
        public TargetGroup TargetGroup { get; set; } = default!;

        // 當頁面以 GET 方式請求時執行，用於載入要編輯的資料
        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // 使用 FindAsync 根據主鍵 id 查詢資料
            var targetgroup = await _context.TargetGroups.FindAsync(id);

            if (targetgroup == null)
            {
                return NotFound();
            }
            TargetGroup = targetgroup;
            return Page();
        }

        // 當表單以 POST 方式提交時執行，用於儲存變更
        public async Task<IActionResult> OnPostAsync()
        {
            // 清除不需要模型繫結的導覽屬性狀態
            ModelState.Remove("TargetGroup.TargetUsers");

            if (!ModelState.IsValid)
            {
                return Page(); // 返回頁面顯示驗證錯誤
            }

            // --- 使用 Fetch-Then-Update 方式處理更新，更安全 ---
            var groupToUpdate = await _context.TargetGroups.FindAsync(TargetGroup.Id);

            if (groupToUpdate == null)
            {
                // 如果在 POST 過程中，資料已被刪除，返回 NotFound
                return NotFound();
            }

            // 使用 TryUpdateModelAsync 將繫結的 TargetGroup (來自表單) 的值更新到從資料庫讀取的 groupToUpdate
            // 第二個參數是前綴 (通常是模型屬性名稱)，第三個參數指定只更新哪些屬性
            if (await TryUpdateModelAsync<TargetGroup>(
                groupToUpdate,
                "TargetGroup", // 與 [BindProperty] 的屬性名稱一致
                g => g.Name, g => g.Description)) // 只更新 Name 和 Description
            {
                groupToUpdate.UpdateTime = DateTime.UtcNow; // 更新修改時間
                try
                {
                    await _context.SaveChangesAsync(); // 儲存變更
                }
                catch (DbUpdateConcurrencyException)
                {
                    // 處理併發衝突 (例如，在您讀取後，其他人修改了同一筆資料)
                    if (!TargetGroupExists(TargetGroup.Id))
                    {
                        return NotFound(); // 如果資料已被刪除
                    }
                    else
                    {
                        // 可以選擇性地加入模型錯誤，提示使用者資料已被修改
                        ModelState.AddModelError(string.Empty, "此資料已被其他人修改，請重新載入後再試。");
                        return Page();
                        // 或者直接拋出例外 throw;
                    }
                }
                return RedirectToPage("./Index"); // 更新成功後導向列表頁
            }

            // 如果 TryUpdateModelAsync 失敗 (雖然較少見)，返回頁面
            return Page();


            /* --- 較不建議的 Attach & Modify 方式 ---
            // 這種方式較簡單，但如果 TargetGroup 物件包含未從表單提交的屬性(例如 CreateTime)，
            // 且這些屬性在模型繫結時被設為預設值，可能會導致原始值被覆蓋。
            TargetGroup.UpdateTime = DateTime.UtcNow; // 更新修改時間
            _context.Attach(TargetGroup).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TargetGroupExists(TargetGroup.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToPage("./Index");
            */
        }

        // 輔助方法：檢查具有指定 id 的 TargetGroup 是否存在
        private bool TargetGroupExists(int id)
        {
            return _context.TargetGroups.Any(e => e.Id == id);
        }
    }
}