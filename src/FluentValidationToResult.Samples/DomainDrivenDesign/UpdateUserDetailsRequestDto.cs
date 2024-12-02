using FluentResults;
using FluentValidation;
using FluentValidationToResult.FluentValidation;
using FluentValidationToResult.Samples.DomainDrivenDesign.DomainObjects;

namespace FluentValidationToResult.Samples.DomainDrivenDesign;
public record UpdateUserDetailsRequestDto(
    string? FirstName,
    string? LastName,
    string? Email,
    string? Phone,
    string? Address
);

public static class UpdateUserDetailsRequestDtoExtensions
{
    public static Result<UpdateCandidateDetailsCommand> TryToCommand(this UpdateUserDetailsRequestDto self, UserId userId)
    {
        // Define inline validation rules if quite localised, or define validator elsewhere if reusable
        var validator = new InlineValidator<UpdateUserDetailsRequestDto>();

        validator.RuleFor(x => x.FirstName).EnsureOptionalResult(NonEmptyString.TryCreate);
        validator.RuleFor(x => x.LastName).EnsureOptionalResult(NonEmptyString.TryCreate);
        validator.RuleFor(x => x.Email).EnsureOptionalResult(EmailAddress.TryCreate);
        validator.RuleFor(x => x.Phone).EnsureOptionalResult(MobilePhoneNumber.TryCreate);
        validator.RuleFor(x => x.Address).EnsureOptionalResult(NonEmptyString.TryCreate);

        // The key moment of the library where validation results are contained and can be extracted to Result<T>/Result types
        var validationContextResult = validator.ValidateToContextResult(self);

        // Map results to command through aforementioned method
        return validationContextResult
            .ValidationResult
            .Bind(_ => Result.Ok(
                new UpdateCandidateDetailsCommand(
                    UserId: userId,
                    FirstName: validationContextResult.ExpectValue<NonEmptyString>(x => x.FirstName),
                    LastName: validationContextResult.ExpectValue<NonEmptyString>(x => x.LastName),
                    Email: validationContextResult.ExpectValue<EmailAddress>(x => x.Email),
                    Phone: validationContextResult.ExpectValue<MobilePhoneNumber>(x => x.Phone),
                    Address: validationContextResult.ExpectValueOrDefault<NonEmptyString>(x => x.Address)
                )));
    }
}
