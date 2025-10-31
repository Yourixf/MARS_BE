namespace MARS_BE.Features.Employees;

public class Employee
{
    public Guid Id { get; set; }
    public string EmployeeNo { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName  { get; set; } = "";
    public string Email     { get; set; } = "";
    public DateTime HireDate { get; set; }

    // Soft delete / status
    public bool IsActive { get; set; } = true;

    // Voor later (custom velden per org)
    // public Dictionary<string, object> Custom { get; set; } = new();
}