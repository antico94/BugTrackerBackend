// DTOs/ExcelReportDto.cs

using BugTracker.Models.Enums;

namespace BugTracker.DTOs;

public class WeeklyCoreBugsExcelReportDto
{
    public string WeeklyCoreBugsName { get; set; }
    public DateTime WeekStartDate { get; set; }
    public DateTime WeekEndDate { get; set; }
    public List<CoreBugSheetDto> CoreBugSheets { get; set; } = new List<CoreBugSheetDto>();
}

public class CoreBugSheetDto
{
    public string JiraKey { get; set; }
    public string BugTitle { get; set; }
    public List<CoreBugTaskRowDto> TaskRows { get; set; } = new List<CoreBugTaskRowDto>();
}

public class CoreBugTaskRowDto
{
    public string JiraKey { get; set; }
    public string Study { get; set; }
    public string IRTVersion { get; set; } // Will show TM version if it's a TM task
    public bool IsImpacted { get; set; }
    public string ShortExplanation { get; set; }
    public string Resolution { get; set; }
    public ProductType ProductType { get; set; }
}