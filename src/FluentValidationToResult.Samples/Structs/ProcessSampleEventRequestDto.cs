using FluentResults;
using FluentValidation;
using FluentValidationToResult.FluentValidation;
using NodaTime;

namespace FluentValidationToResult.Samples.Structs;
public record ProcessSampleEventRequestDto(
    string? NullableDate
);

public static class ProcessSampleEventRequestDtoExtensions
{
    public static Result<ProcessSampleEventCommand> CorrectUsage(this ProcessSampleEventRequestDto self)
    {
        var validator = new InlineValidator<ProcessSampleEventRequestDto>();

        validator.RuleFor(x => x.NullableDate).EnsureOptionalResult(x => x.TryToNullableLocalDate());

        var validationContextResult = validator.ValidateToContextResult(self);

        return new ProcessSampleEventCommand(
            NullableDate: validationContextResult.ExpectValue<LocalDate?>(x => x.NullableDate));
    }

    public static Result<ProcessSampleEventCommand> IncorrectUsage(this ProcessSampleEventRequestDto self)
    {
        var validator = new InlineValidator<ProcessSampleEventRequestDto>();

        // Herein lies a crucial error. Ensure your struct factory returns a nullable struct when using EnsureOptionalResult
        // If you do not, instead of a null struct, you will get the default struct ("0001-01-01" in this case) and lead to nasty bugs
        validator.RuleFor(x => x.NullableDate).EnsureOptionalResult(x => x.MethodThatReturnsNonNullableStruct());

        var validationContextResult = validator.ValidateToContextResult(self);

        return new ProcessSampleEventCommand(
            // Though the compiler will allow <LocalDate> not specifying a nullable T
            // always use ExpectValueOrDefault<T?> for clarity and to prevent odd behaviour
            NullableDate: validationContextResult.ExpectValueOrDefault<LocalDate?>(x => x.NullableDate));
    }
}