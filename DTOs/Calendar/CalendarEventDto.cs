namespace EMS.Api.DTOs.Calendar;

public class CalendarEventDto
{
    public required string Date { get; set; } // Format "YYYY-MM-DD" for holidays, "MM-DD" for birthdays
    public required string Description { get; set; }
    public required string Type { get; set; } // "holiday" or "birthday"
}