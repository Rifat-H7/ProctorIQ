using FluentValidation;
using ProctorIQ.Application.Auth;
using ProctorIQ.Application.Exams;

namespace ProctorIQ.Application.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
    }
}

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class CreateExamRequestValidator : AbstractValidator<CreateExamRequest>
{
    public CreateExamRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.DurationMinutes).GreaterThan(0);
        RuleFor(x => x.TotalMarks).GreaterThan(0);
        RuleFor(x => x.PassMarks).GreaterThanOrEqualTo(0);
        RuleFor(x => x).Must(x => x.EndTimeUtc > x.StartTimeUtc).WithMessage("End time must be after start time.");
    }
}
