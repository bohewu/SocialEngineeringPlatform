namespace SocialEngineeringPlatform.Web.Models.Enums
{
    public enum TrackingEventType
    {
        Opened,         // 郵件已開啟
        Clicked,        // 連結已點擊
        SubmittedData,  // 登陸頁已提交資料 (僅記錄動作)
        ReportedPhish   // 使用者回報為釣魚郵件 (若有此功能)
    }
}