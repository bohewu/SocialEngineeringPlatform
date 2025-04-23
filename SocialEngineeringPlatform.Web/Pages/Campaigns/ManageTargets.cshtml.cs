using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SocialEngineeringPlatform.Web.Data;
using SocialEngineeringPlatform.Web.Models.Core;
using SocialEngineeringPlatform.Web.Models.Enums; // For CampaignSendStatus

namespace SocialEngineeringPlatform.Web.Pages.Campaigns
{
    // [Authorize]
    public class ManageTargetsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ManageTargetsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // 從路由或查詢字串綁定 CampaignId
        [BindProperty(SupportsGet = true)]
        public int CampaignId { get; set; }

        // 顯示活動基本資訊
        public Campaign Campaign { get; set; } = default!;

        // 顯示目前已加入的目標
        public List<TargetUser> CurrentTargets { get; set; } = new List<TargetUser>();

        // 顯示可供加入的目標
        public List<TargetUser> AvailableTargetUsers { get; set; } = new List<TargetUser>();

        // 綁定從表單提交過來的要新增的目標使用者 ID 列表
        [BindProperty]
        public List<int> TargetsToAdd { get; set; } = new List<int>();

        // OnGet 用於載入活動資訊、目前目標和可用目標
        public async Task<IActionResult> OnGetAsync()
        {
            // 查詢活動基本資訊
            Campaign = await _context.Campaigns
                             .AsNoTracking()
                             .FirstOrDefaultAsync(c => c.Id == CampaignId);

            if (Campaign == null)
            {
                return NotFound($"找不到 ID 為 {CampaignId} 的活動。");
            }

            // 查詢目前已加入的目標 ID
            var currentTargetIds = await _context.CampaignTargets
                                         .Where(ct => ct.CampaignId == CampaignId)
                                         .Select(ct => ct.TargetUserId)
                                         .ToListAsync();

            // 查詢目前已加入的目標使用者詳細資料
            if (currentTargetIds.Any())
            {
                // 同時載入 Group 資訊以便在 AvailableTargetUsers 中顯示
                CurrentTargets = await _context.TargetUsers
                                       .Where(tu => currentTargetIds.Contains(tu.Id))
                                       .AsNoTracking()
                                       .ToListAsync();
            }

            // 查詢所有可用的目標使用者 (排除已加入的)，並載入 Group 資訊
            AvailableTargetUsers = await _context.TargetUsers
                                       .Include(tu => tu.Group) // *** 載入 Group 資訊 ***
                                       .Where(tu => tu.IsActive && !currentTargetIds.Contains(tu.Id)) // 只顯示啟用且未加入的
                                       .OrderBy(tu => tu.Email) // 依照 Email 排序
                                       .AsNoTracking()
                                       .ToListAsync();

            return Page();
        }

        // OnPost (或可改名 OnPostAddTargetsAsync) 用於處理新增選取目標的提交
        public async Task<IActionResult> OnPostAsync() // 或者 OnPostAddTargetsAsync
        {
             // 先載入活動資訊，以便知道是哪個活動
            Campaign = await _context.Campaigns.FindAsync(CampaignId);
             if (Campaign == null)
            {
                return NotFound($"找不到 ID 為 {CampaignId} 的活動。");
            }

            if (TargetsToAdd != null && TargetsToAdd.Any())
            {
                // 取得目前已加入的目標 ID，避免重複加入
                var currentTargetIds = await _context.CampaignTargets
                                             .Where(ct => ct.CampaignId == CampaignId)
                                             .Select(ct => ct.TargetUserId)
                                             .ToHashSetAsync();

                var newTargets = new List<CampaignTarget>();
                foreach (var userIdToAdd in TargetsToAdd)
                {
                    // 檢查是否已存在，且使用者存在於 TargetUsers 表中
                    if (!currentTargetIds.Contains(userIdToAdd) && await _context.TargetUsers.AnyAsync(u => u.Id == userIdToAdd))
                    {
                        newTargets.Add(new CampaignTarget
                        {
                            CampaignId = CampaignId,
                            TargetUserId = userIdToAdd,
                            SendStatus = CampaignSendStatus.Pending // 預設狀態為待發送
                        });
                    }
                }

                if (newTargets.Any())
                {
                    _context.CampaignTargets.AddRange(newTargets);
                    await _context.SaveChangesAsync();
                    // TempData["SuccessMessage"] = $"成功將 {newTargets.Count} 個目標加入活動。";
                }
            }
            else
            {
                 // TempData["InfoMessage"] = "您沒有選取任何要新增的目標。";
            }

            // 操作完成後，重新導向回同一個頁面，顯示更新後的列表
            return RedirectToPage(new { campaignId = CampaignId });
        }

        // *** 新增：處理移除目標的 Handler ***
        public async Task<IActionResult> OnPostRemoveTargetAsync(int userId)
        {
            // 查找要移除的 CampaignTarget 記錄
            var campaignTargetToRemove = await _context.CampaignTargets
                                               .FirstOrDefaultAsync(ct => ct.CampaignId == CampaignId && ct.TargetUserId == userId);

            if (campaignTargetToRemove != null)
            {
                // TODO: 根據業務邏輯，可能需要檢查此目標是否已發送郵件或已有追蹤事件，
                //       如果有關聯記錄，可能不允許移除，或需要做其他處理。
                //       此處為簡化範例，直接移除。

                _context.CampaignTargets.Remove(campaignTargetToRemove);
                await _context.SaveChangesAsync();
                // TempData["SuccessMessage"] = "已成功從活動移除目標。";
            }
            else
            {
                // TempData["ErrorMessage"] = "找不到要移除的目標關聯。";
            }

             // 操作完成後，重新導向回同一個頁面
            return RedirectToPage(new { campaignId = CampaignId });
        }
    }
}