using FluentValidation;

namespace MARS_BE.Features.Employees;

public sealed class EmployeeCreateValidator : AbstractValidator<EmployeeCreateDto>
{
    public EmployeeCreateValidator()
    {
        RuleFor(x => x.EmployeeNo).NotEmpty().MaximumLength(50);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.HireDate).LessThanOrEqualTo(DateTime.UtcNow.AddDays(1));
    }
}

public sealed class EmployeeUpdateValidator : AbstractValidator<EmployeeUpdateDto>
{
    public EmployeeUpdateValidator()
    {
        RuleFor(x => x.FirstName).MaximumLength(100);
        RuleFor(x => x.LastName).MaximumLength(100);
        RuleFor(x => x.Email).EmailAddress().When(x => x.Email is not null).MaximumLength(200);
        RuleFor(x => x.HireDate).LessThanOrEqualTo(DateTime.UtcNow.AddDays(1))
            .When(x => x.HireDate is not null);
    }
}