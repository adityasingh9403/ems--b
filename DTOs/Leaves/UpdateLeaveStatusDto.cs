using System.ComponentModel.DataAnnotations;

namespace EMS.Api.DTOs.Leaves;

public class UpdateLeaveStatusDto
{
    [Required]
    [RegularExpression("^(approved|rejected)$", ErrorMessage = "Status must be 'approved' or 'rejected'.")]
    public required string Status { get; set; }
}
