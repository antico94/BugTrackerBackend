// Services/ExcelReportService.cs
using BugTracker.Models;
using BugTracker.Models.Enums;
using ClosedXML.Excel;
using System.Text;

namespace BugTracker.Services;

public class ExcelReportService
{
    private readonly ILogger<ExcelReportService> _logger;

    public ExcelReportService(ILogger<ExcelReportService> logger)
    {
        _logger = logger;
    }

    public async Task<byte[]> GenerateWeeklyCoreBugsReport(WeeklyCoreBugs weeklyCoreBugs)
    {
        try
        {
            using var workbook = new XLWorkbook();

            // Create a summary sheet first
            CreateSummarySheet(workbook, weeklyCoreBugs);

            // Create a sheet for each CoreBug
            foreach (var entry in weeklyCoreBugs.WeeklyCoreBugEntries)
            {
                var coreBug = entry.CoreBug;
                if (coreBug == null) continue;

                var sheetName = SanitizeSheetName(coreBug.JiraKey);
                var worksheet = workbook.Worksheets.Add(sheetName);

                // Set up the worksheet for this CoreBug
                SetupCoreBugSheet(worksheet, coreBug);
            }

            // Save to memory stream
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Excel report for WeeklyCoreBugs {WeeklyCoreBugsId}", weeklyCoreBugs.WeeklyCoreBugsId);
            throw;
        }
    }

    private void CreateSummarySheet(XLWorkbook workbook, WeeklyCoreBugs weeklyCoreBugs)
    {
        var summarySheet = workbook.Worksheets.Add("Summary");

        // Title and metadata
        summarySheet.Cell(1, 1).Value = "Weekly Core Bugs Report";
        summarySheet.Cell(1, 1).Style.Font.Bold = true;
        summarySheet.Cell(1, 1).Style.Font.FontSize = 16;

        summarySheet.Cell(2, 1).Value = $"Week: {weeklyCoreBugs.Name}";
        summarySheet.Cell(3, 1).Value = $"Period: {weeklyCoreBugs.WeekStartDate:yyyy-MM-dd} to {weeklyCoreBugs.WeekEndDate:yyyy-MM-dd}";
        summarySheet.Cell(4, 1).Value = $"Status: {weeklyCoreBugs.Status}";
        summarySheet.Cell(5, 1).Value = $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";

        // Statistics
        var totalBugs = weeklyCoreBugs.WeeklyCoreBugEntries.Count;
        var assessedBugs = weeklyCoreBugs.WeeklyCoreBugEntries.Count(e => e.CoreBug?.IsAssessed == true);
        var totalTasks = weeklyCoreBugs.WeeklyCoreBugEntries.SelectMany(e => e.CoreBug?.Tasks ?? new List<CustomTask>()).Count();
        var completedTasks = weeklyCoreBugs.WeeklyCoreBugEntries
            .SelectMany(e => e.CoreBug?.Tasks ?? new List<CustomTask>())
            .Count(t => t.Status == Status.Done);

        summarySheet.Cell(7, 1).Value = "Statistics:";
        summarySheet.Cell(7, 1).Style.Font.Bold = true;

        summarySheet.Cell(8, 1).Value = "Total Bugs:";
        summarySheet.Cell(8, 2).Value = totalBugs;

        summarySheet.Cell(9, 1).Value = "Assessed Bugs:";
        summarySheet.Cell(9, 2).Value = assessedBugs;

        summarySheet.Cell(10, 1).Value = "Total Tasks:";
        summarySheet.Cell(10, 2).Value = totalTasks;

        summarySheet.Cell(11, 1).Value = "Completed Tasks:";
        summarySheet.Cell(11, 2).Value = completedTasks;

        summarySheet.Cell(12, 1).Value = "Completion Rate:";
        summarySheet.Cell(12, 2).Value = totalTasks > 0 ? $"{Math.Round((double)completedTasks / totalTasks * 100, 1)}%" : "0%";

        // Bug List
        summarySheet.Cell(14, 1).Value = "Core Bugs in this Week:";
        summarySheet.Cell(14, 1).Style.Font.Bold = true;

        // Headers for bug list
        var headerRow = 15;
        summarySheet.Cell(headerRow, 1).Value = "JIRA Key";
        summarySheet.Cell(headerRow, 2).Value = "Title";
        summarySheet.Cell(headerRow, 3).Value = "Severity";
        summarySheet.Cell(headerRow, 4).Value = "Status";
        summarySheet.Cell(headerRow, 5).Value = "Assessed";
        summarySheet.Cell(headerRow, 6).Value = "Product Type";
        summarySheet.Cell(headerRow, 7).Value = "Tasks";
        summarySheet.Cell(headerRow, 8).Value = "Completed Tasks";

        // Style headers
        var headerRange = summarySheet.Range(headerRow, 1, headerRow, 8);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
        headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        // Data rows
        var currentRow = headerRow + 1;
        foreach (var entry in weeklyCoreBugs.WeeklyCoreBugEntries.OrderBy(e => e.CoreBug?.JiraKey))
        {
            var bug = entry.CoreBug;
            if (bug == null) continue;

            summarySheet.Cell(currentRow, 1).Value = bug.JiraKey;
            summarySheet.Cell(currentRow, 2).Value = TruncateText(bug.BugTitle, 50);
            summarySheet.Cell(currentRow, 3).Value = bug.Severity.ToString();
            summarySheet.Cell(currentRow, 4).Value = bug.Status.ToString();
            summarySheet.Cell(currentRow, 5).Value = bug.IsAssessed ? "Yes" : "No";
            summarySheet.Cell(currentRow, 6).Value = bug.AssessedProductType?.ToString() ?? "Not Assessed";
            summarySheet.Cell(currentRow, 7).Value = bug.Tasks?.Count ?? 0;
            summarySheet.Cell(currentRow, 8).Value = bug.Tasks?.Count(t => t.Status == Status.Done) ?? 0;

            currentRow++;
        }

        // Auto-fit columns
        summarySheet.Columns().AdjustToContents();
    }

    private void SetupCoreBugSheet(IXLWorksheet worksheet, CoreBug coreBug)
    {
        // Title
        worksheet.Cell(1, 1).Value = $"Bug: {coreBug.JiraKey}";
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 14;

        worksheet.Cell(2, 1).Value = coreBug.BugTitle;
        worksheet.Cell(2, 1).Style.Font.FontSize = 12;

        // Bug details
        worksheet.Cell(4, 1).Value = "Severity:";
        worksheet.Cell(4, 1).Style.Font.Bold = true;
        worksheet.Cell(4, 2).Value = coreBug.Severity.ToString();

        worksheet.Cell(5, 1).Value = "Status:";
        worksheet.Cell(5, 1).Style.Font.Bold = true;
        worksheet.Cell(5, 2).Value = coreBug.Status.ToString();

        worksheet.Cell(6, 1).Value = "JIRA Link:";
        worksheet.Cell(6, 1).Style.Font.Bold = true;
        worksheet.Cell(6, 2).Value = coreBug.JiraLink;

        // Task table headers
        var headerRow = 8;
        worksheet.Cell(headerRow, 1).Value = "JIRA Key";
        worksheet.Cell(headerRow, 2).Value = "Study";
        worksheet.Cell(headerRow, 3).Value = "Version";
        worksheet.Cell(headerRow, 4).Value = "Is Impacted";
        worksheet.Cell(headerRow, 5).Value = "Short Explanation";
        worksheet.Cell(headerRow, 6).Value = "Resolution";

        // Style headers
        var headerRange = worksheet.Range(headerRow, 1, headerRow, 6);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
        headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        // Data rows
        var row = headerRow + 1;
        
        if (coreBug.Tasks?.Any() == true)
        {
            foreach (var task in coreBug.Tasks.OrderBy(t => t.CreatedAt))
            {
                // JIRA Key column
                worksheet.Cell(row, 1).Value = !string.IsNullOrEmpty(task.JiraTaskKey) ? task.JiraTaskKey : coreBug.JiraKey;

                // Study column
                string studyName = "Unknown";
                if (task.TrialManager?.Client != null)
                {
                    studyName = task.TrialManager.Client.Name;
                }
                else if (task.InteractiveResponseTechnology?.Study != null)
                {
                    studyName = task.InteractiveResponseTechnology.Study.Name;
                }
                else if (task.Study != null)
                {
                    studyName = task.Study.Name;
                }
                worksheet.Cell(row, 2).Value = studyName;

                // Version column
                string version = "Unknown";
                if (task.TrialManager != null)
                {
                    version = task.TrialManager.Version;
                }
                else if (task.InteractiveResponseTechnology != null)
                {
                    version = task.InteractiveResponseTechnology.Version;
                }
                worksheet.Cell(row, 3).Value = version;

                // Is Impacted column (has JiraTaskKey means it was cloned/impacted)
                bool isImpacted = !string.IsNullOrEmpty(task.JiraTaskKey);
                worksheet.Cell(row, 4).Value = isImpacted ? "Yes" : "No";

                // Short Explanation column
                string explanation;
                if (isImpacted)
                {
                    explanation = "This version is impacted by the core bug.";
                }
                else
                {
                    explanation = "This version is not impacted by the core bug affected versions.";
                }
                worksheet.Cell(row, 5).Value = explanation;

                // Resolution column
                string resolution = GetTaskResolution(task, isImpacted);
                worksheet.Cell(row, 6).Value = resolution;

                row++;
            }
        }
        else
        {
            // No tasks - show message
            worksheet.Cell(row, 1).Value = "No tasks generated";
            worksheet.Cell(row, 2).Value = "Bug not assessed or no impacted products";
            worksheet.Range(row, 1, row, 6).Merge();
            worksheet.Cell(row, 1).Style.Font.Italic = true;
            worksheet.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.LightYellow;
        }

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();

        // Set column widths for better readability
        worksheet.Column(1).Width = 15; // JIRA Key
        worksheet.Column(2).Width = 25; // Study
        worksheet.Column(3).Width = 12; // Version
        worksheet.Column(4).Width = 12; // Is Impacted
        worksheet.Column(5).Width = 40; // Short Explanation
        worksheet.Column(6).Width = 30; // Resolution
    }

    private string GetTaskResolution(CustomTask task, bool isImpacted)
    {
        if (!isImpacted)
        {
            return "Don't Clone It";
        }

        return task.Status switch
        {
            Status.Done => "Task completed successfully",
            Status.InProgress => GetTaskProgressDetails(task),
            Status.New => "Task created but not started",
            _ => "Unknown status"
        };
    }

    private string GetTaskProgressDetails(CustomTask task)
    {
        if (task.TaskSteps?.Any() == true)
        {
            var completedSteps = task.TaskSteps.Count(ts => ts.Status == Status.Done);
            var totalSteps = task.TaskSteps.Count;
            return $"In Progress ({completedSteps}/{totalSteps} steps completed)";
        }
        return "Task in progress";
    }

    private string SanitizeSheetName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return "Sheet";

        // Excel sheet names can't contain certain characters
        var invalid = new char[] { '/', '\\', '?', '*', '[', ']', ':' };
        var sanitized = name;
        
        foreach (var c in invalid)
        {
            sanitized = sanitized.Replace(c, '_');
        }

        // Excel sheet names have a 31 character limit
        if (sanitized.Length > 31)
        {
            sanitized = sanitized.Substring(0, 31);
        }

        return sanitized;
    }

    private string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text ?? "";

        return text.Substring(0, maxLength - 3) + "...";
    }
}