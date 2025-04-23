using Microsoft.AspNetCore.Identity;

namespace SocialEngineeringPlatform.Web.Models.Core;

public class ApplicationUser : IdentityUser
{
    // 導覽屬性: 建立的活動、郵件範本、登陸頁範本
    public virtual ICollection<Campaign> CreatedCampaigns { get; set; } = new List<Campaign>();
    public virtual ICollection<MailTemplate> CreatedMailTemplates { get; set; } = new List<MailTemplate>();
    public virtual ICollection<LandingPageTemplate> CreatedLandingPageTemplates { get; set; } = new List<LandingPageTemplate>();
}