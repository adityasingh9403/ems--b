namespace EMS.Api.DTOs.Performance;

public class EmployeeRankingDto
{
    public int EmployeeId { get; set; }
    public required string FullName { get; set; }
    public required string Designation { get; set; }
    public int Score { get; set; }
    
    // Metrics for display
    public int PresentDays { get; set; }
    public int LateDays { get; set; }
    public int AbsentDays { get; set; }
    public int TasksCompleted { get; set; }
    public int LeaveDays { get; set; }
}