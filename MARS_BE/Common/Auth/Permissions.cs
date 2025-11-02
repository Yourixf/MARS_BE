namespace MARS_BE.Common.Auth;

public static class Permissions
{
    // Keep permissions granular (claim type "perm")
    public const string EmployeesRead  = "employees.read";
    public const string EmployeesWrite = "employees.write";
    public const string ClientsRead    = "clients.read";
    public const string ClientsWrite   = "clients.write";
}