using FluentValidationToResult.Samples.DomainDrivenDesign.DomainObjects;

namespace FluentValidationToResult.Samples.DomainDrivenDesign;
public record UpdateCandidateDetailsCommand(
    UserId UserId,
    NonEmptyString FirstName,
    NonEmptyString LastName,
    EmailAddress Email,
    MobilePhoneNumber Phone,
    NonEmptyString? Address
);