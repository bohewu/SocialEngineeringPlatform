namespace SocialEngineeringPlatform.Web.ViewModels;

public class CampaignExecutionResult
{
    public bool Success { get; set; } = false;
    public int SuccessCount { get; set; } = 0;
    public int FailCount { get; set; } = 0;
    public string Message { get; set; } = string.Empty;
}