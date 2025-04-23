using System.ComponentModel.DataAnnotations; // For ValidationContext, ValidationResult
using System.Globalization; // For CultureInfo
// For StreamReader, MemoryStream
using System.Text; // For Encoding
using CsvHelper; // CsvHelper using
using CsvHelper.Configuration; // For CsvConfiguration
// For IFormFile
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SocialEngineeringPlatform.Web.Data;
using SocialEngineeringPlatform.Web.Models.Core;
using SocialEngineeringPlatform.Web.ViewModels;

namespace SocialEngineeringPlatform.Web.Pages.TargetUsers
{
    // [Authorize]
    public class ImportModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ImportModel> _logger;

        public ImportModel(ApplicationDbContext context, ILogger<ImportModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        // 綁定上傳的檔案
        [BindProperty] public IFormFile? UploadedFile { get; set; }

        // 顯示匯入結果
        public bool ImportAttempted { get; set; } = false; // 是否已嘗試匯入
        public int SuccessCount { get; set; } = 0;
        public int FailCount { get; set; } = 0;
        public List<ImportTargetUserViewModel> FailedRows { get; set; } = new List<ImportTargetUserViewModel>();

        // GET 請求只顯示頁面
        public void OnGet()
        {
        }

        // POST 請求處理檔案上傳與匯入
        public async Task<IActionResult> OnPostAsync()
        {
            ImportAttempted = true; // 標記已嘗試匯入

            if (UploadedFile == null || UploadedFile.Length == 0)
            {
                ModelState.AddModelError("UploadedFile", "請選擇要上傳的 CSV 檔案。");
                return Page();
            }

            // 限制檔案類型 (可選)
            if (!UploadedFile.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("UploadedFile", "僅支援 CSV 格式的檔案。");
                return Page();
            }

            var usersToImport = new List<ImportTargetUserViewModel>();
            var existingEmails =
                await _context.TargetUsers.Select(u => u.Email.ToLower()).ToHashSetAsync(); // 取得現有 Email (轉小寫)
            var groupNameToIdMap =
                await _context.TargetGroups.ToDictionaryAsync(g => g.Name.ToLower(), g => g.Id); // 群組名稱到 ID 的映射 (轉小寫)

            // --- 使用 CsvHelper 讀取檔案 ---
            try
            {
                // 設定 CsvHelper
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true, // CSV 有標頭行
                    HeaderValidated = null, // 不嚴格檢查標頭名稱，但會記錄錯誤
                    MissingFieldFound = null, // 允許缺少欄位 (會是 null)
                    TrimOptions = TrimOptions.Trim, // 去除欄位前後空白
                    PrepareHeaderForMatch =
                        args => args.Header.ToLower().Replace("_", "").Replace(" ", ""), // 標頭比對時轉小寫並移除底線/空白
                };

                using (var reader = new StreamReader(UploadedFile.OpenReadStream(), Encoding.UTF8)) // 使用 UTF-8 讀取
                using (var csv = new CsvReader(reader, config))
                {
                    csv.Context.RegisterClassMap<ImportTargetUserViewModelMap>(); // 使用自訂映射 (可選)
                    int rowNum = 1; // 從第 1 行資料開始 (標頭後)
                    await foreach (var record in csv.GetRecordsAsync<ImportTargetUserViewModel>())
                    {
                        rowNum++;
                        record.RowNumber = rowNum; // 記錄行號

                        // --- 執行驗證 ---
                        var validationContext = new ValidationContext(record);
                        var validationResults = new List<ValidationResult>();
                        bool isModelValid = Validator.TryValidateObject(record, validationContext, validationResults,
                            validateAllProperties: true);
                        if (!isModelValid)
                        {
                            record.IsValid = false;
                            record.Errors.AddRange(validationResults.Select(r => r.ErrorMessage ?? "未知模型驗證錯誤"));
                        }

                        if (string.IsNullOrWhiteSpace(record.Email))
                        {
                            record.IsValid = false;
                            record.Errors.Add("Email 不得為空。");
                        }
                        else if (existingEmails.Contains(record.Email.ToLower()) ||
                                 usersToImport.Any(u => u.IsValid && u.Email?.ToLower() == record.Email.ToLower()))
                        {
                            record.IsValid = false;
                            record.Errors.Add($"Email '{record.Email}' 已存在。");
                        }

                        if (!string.IsNullOrWhiteSpace(record.GroupName) &&
                            !groupNameToIdMap.ContainsKey(record.GroupName.ToLower()))
                        {
                            record.IsValid = false;
                            record.Errors.Add($"找不到名為 '{record.GroupName}' 的目標群組。");
                        }

                        usersToImport.Add(record);
                    }
                }
            }
            catch (HeaderValidationException hvEx)
            {
                /* ... 錯誤處理 ... */
                _logger.LogError(hvEx, "CSV 標頭驗證失敗。");
                ModelState.AddModelError("UploadedFile", $"CSV 標頭錯誤: {hvEx.Message}");
                return Page();
            }
            catch (Exception ex)
            {
                /* ... 錯誤處理 ... */
                _logger.LogError(ex, "讀取或解析 CSV 檔案時發生錯誤。");
                ModelState.AddModelError("UploadedFile", $"處理檔案時發生錯誤: {ex.Message}");
                return Page();
            }

            // --- 處理驗證結果並儲存資料 ---
            var usersToAdd = new List<TargetUser>();
            foreach (var record in usersToImport)
            {
                if (record.IsValid)
                {
                    SuccessCount++;
                    int? groupId = null;
                    if (!string.IsNullOrWhiteSpace(record.GroupName) &&
                        groupNameToIdMap.TryGetValue(record.GroupName.ToLower(), out int foundGroupId))
                    {
                        groupId = foundGroupId;
                    }

                    usersToAdd.Add(new TargetUser
                    {
                        /* ... 屬性賦值 ... */ Email = record.Email!, Name = record.Name, GroupId = groupId,
                        CustomField1 = record.CustomField1,
                        CustomField2 = record.CustomField2,
                        IsActive = true,
                        CreateTime = DateTime.UtcNow, 
                        UpdateTime = DateTime.UtcNow
                    });
                    existingEmails.Add(record.Email!.ToLower());
                }
                else
                {
                    FailCount++;
                    FailedRows.Add(record);
                }
            }

            if (usersToAdd.Any())
            {
                try
                {
                    _context.TargetUsers.AddRange(usersToAdd);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("成功匯入 {Count} 個目標使用者。", usersToAdd.Count);
                }
                catch (Exception ex)
                {
                    /* ... 資料庫錯誤處理 ... */
                    _logger.LogError(ex, "儲存匯入的目標使用者時發生資料庫錯誤。");
                    FailCount += SuccessCount;
                    SuccessCount = 0;
                    FailedRows.AddRange(usersToImport.Where(u => u.IsValid).Select(u =>
                    {
                        u.IsValid = false;
                        u.Errors.Add($"資料庫儲存失敗: {ex.Message}");
                        return u;
                    }));
                    ModelState.AddModelError(string.Empty, "儲存資料時發生錯誤，部分或全部資料可能未匯入。");
                }
            }

            // 設定 TempData 訊息
            if (SuccessCount > 0)
            {
                TempData["SuccessMessage"] = $"成功匯入 {SuccessCount} 筆資料。";
            }

            if (FailCount > 0)
            {
                TempData["ErrorMessage"] = $"有 {FailCount} 筆資料匯入失敗，請檢查下方錯誤詳情。";
            }

            if (SuccessCount == 0 && FailCount == 0)
            {
                TempData["InfoMessage"] = "CSV 檔案中沒有找到有效的資料可供匯入。";
            }

            return Page();
        }

        // 下載 CSV 樣板的 Handler
        public IActionResult OnGetDownloadTemplate()
        {
            // *** 修改：定義中文標頭行 ***
            var headers = new List<string> { "電子郵件", "姓名", "所屬群組名稱", "自訂欄位1", "自訂欄位2" };
            var headerString = string.Join(",", headers); // 用逗號分隔

            var csvContent = headerString; // 僅標頭

            // 將字串轉換為位元組陣列 (使用 UTF-8 with BOM)
            var fileBytes = Encoding.UTF8.GetBytes(csvContent);
            var bom = Encoding.UTF8.GetPreamble();
            var bytesWithBom = bom.Concat(fileBytes).ToArray();

            // 返回檔案結果
            return File(bytesWithBom, "text/csv", "目標使用者匯入樣板.csv"); // 修改檔名
        }


        // --- (可選) CsvHelper 自訂映射 ---
        public sealed class ImportTargetUserViewModelMap : ClassMap<ImportTargetUserViewModel>
        {
            public ImportTargetUserViewModelMap()
            {
                AutoMap(CultureInfo.InvariantCulture);
                Map(m => m.Email).Name("電子郵件");
                Map(m => m.Name).Name("姓名");
                Map(m => m.GroupName).Name("所屬群組名稱");
                Map(m => m.CustomField1).Name("自訂欄位1");
                Map(m => m.CustomField1).Name("自訂欄位2");
            }
        }
    }
}