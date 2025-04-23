using SocialEngineeringPlatform.Web.Models.Core;
using SocialEngineeringPlatform.Web.Models.Enums;

namespace SocialEngineeringPlatform.Web.ViewModels
{
    // 用於傳遞儀表板所需數據的 ViewModel
    public class DashboardViewModel
    {
        public int TotalCampaigns { get; set; }
        public Dictionary<CampaignStatus, int> CampaignStatusCounts { get; set; } = new Dictionary<CampaignStatus, int>();
        public List<Campaign> RecentCampaigns { get; set; } = new List<Campaign>();

        // 未來可加入更多統計數據
        // public int TotalTargets { get; set; }
        // public double OverallClickRate { get; set; }
        // public double OverallOpenRate { get; set; }
    }
}