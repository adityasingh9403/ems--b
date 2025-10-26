namespace EMS.Api.DTOs.Dashboard;

public class DashboardStatsDto
{
    // For Admin/HR
    public int? TotalEmployees { get; set; }
    public int? TotalDepartments { get; set; }
    public int? PendingLeaves { get; set; }
    public int? PresentToday { get; set; }

    // For Department Manager
    public int? TeamCount { get; set; }
    public int? TeamPendingLeaves { get; set; }
    public int? TeamPresentToday { get; set; }

    // For Employee
    public int? MyPendingLeaves { get; set; }
    public int? MyTasksPending { get; set; }
}