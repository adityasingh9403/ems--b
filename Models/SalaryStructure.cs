using System.ComponentModel.DataAnnotations.Schema;

namespace EMS.Api.Models;

[Table("payroll_salary_structures")]
public class SalaryStructure
{
    [Column("id")]
    public int Id { get; set; }
    
    // FIX: Changed from EmployeeId to match DB column 'user_id'
    [Column("user_id")]
    public int EmployeeId { get; set; }
    
    // This column will now exist from Step 1
    [Column("company_id")]
    public int CompanyId { get; set; }
    
    [Column("basic")]
    public decimal Basic { get; set; }
    
    [Column("hra")]
    public decimal Hra { get; set; }
    
    [Column("allowances")]
    public decimal Allowances { get; set; }
    
    // FIX: Changed from Pf to match DB column 'pf_deduction'
    [Column("pf_deduction")]
    public decimal Pf { get; set; }
    
    // FIX: Changed from Tax to match DB column 'tax_deduction'
    [Column("tax_deduction")]
    public decimal Tax { get; set; }
}