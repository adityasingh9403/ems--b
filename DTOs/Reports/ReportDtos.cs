namespace EMS.Api.DTOs.Reports;

public class ChartDataItem
{
    public required string Name { get; set; }
    public decimal Value { get; set; }
}

public class AttendanceTrendItem
{
    public required string Date { get; set; }
    public int Present { get; set; }
    public int Late { get; set; }
    public int Absent { get; set; }
}

public class ReportSummaryDto
{
    public required List<ChartDataItem> HeadcountData { get; set; }
    public required List<AttendanceTrendItem> AttendanceTrendData { get; set; }
    public required List<ChartDataItem> LeaveTypeData { get; set; }
    public required List<ChartDataItem> SalaryData { get; set; }
}
