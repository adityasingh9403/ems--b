using System.ComponentModel.DataAnnotations;
using EMS.Api.Models;

namespace EMS.Api.DTOs.Payroll;

public class SalaryStructureDto
{
    [Required]
    public int EmployeeId { get; set; }
    
    public decimal Basic { get; set; }
    public decimal Hra { get; set; }
    public decimal Allowances { get; set; }
    public decimal Pf { get; set; }
    public decimal Tax { get; set; }
    
    // Ye property backend se frontend par jayegi, frontend se aayegi nahi
    public decimal GrossSalary { get; set; }

    // Helper method to convert Model to DTO
    public static SalaryStructureDto FromModel(SalaryStructure structure, decimal grossSalary)
    {
        return new SalaryStructureDto
        {
            EmployeeId = structure.EmployeeId,
            Basic = structure.Basic,
            Hra = structure.Hra,
            Allowances = structure.Allowances,
            Pf = structure.Pf,
            Tax = structure.Tax,
            GrossSalary = grossSalary
        };
    }
}