using System.Collections.Generic;

namespace EMS.Api.DTOs.OrgChart;

public class OrgChartNodeDto
{
    public int Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Role { get; set; }
    public List<OrgChartNodeDto> Children { get; set; } = new List<OrgChartNodeDto>();
}