// DTOs/Attendance/MarkAttendanceDto.cs
namespace EMS.Api.DTOs.Attendance;

public class LocationDto
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public class MarkAttendanceDto
{
    // Ye property clock-in ke waqt frontend se aayegi
    public string? ClockInLocation { get; set; }
    
    // Ye property clock-out ke waqt frontend se aayegi
    public string? ClockOutLocation { get; set; }
}