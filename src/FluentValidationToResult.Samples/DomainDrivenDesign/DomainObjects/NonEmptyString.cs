
using FluentResults;
using FluentValidation;
using FluentValidationToResult.FluentValidation;

namespace FluentValidationToResult.Samples.DomainDrivenDesign.DomainObjects;

public record NonEmptyString
{
    public string Value { get; private set; }

    private NonEmptyString(string value) => Value = value;

    public static Result<NonEmptyString> TryCreate(string value) =>
        new NonEmptyStringValidator().ValidateToResult(new NonEmptyString(value.Trim()));

    public NonEmptyString Prepend(string value) => new NonEmptyString(value + Value);

    public static implicit operator string(NonEmptyString value) => value.Value;
    public static explicit operator NonEmptyString(string value) => TryCreate(value).Value;
    override public string ToString() => Value;
}

public class NonEmptyStringValidator : AbstractValidator<NonEmptyString>
{
    public NonEmptyStringValidator()
    {
        RuleFor(x => x.Value).NotEmpty().WithMessage("must not be empty");
    }
}