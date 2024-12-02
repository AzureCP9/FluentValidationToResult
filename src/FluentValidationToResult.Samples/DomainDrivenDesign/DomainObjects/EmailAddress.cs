using FluentResults;

namespace FluentValidationToResult.Samples.DomainDrivenDesign.DomainObjects;

public record EmailAddress
{
    public string Value { get; }

    private EmailAddress(string value) => Value = value;

    public static Result<EmailAddress> TryCreate(string value) =>
        IsValidEmail(value)
            ? Result.Ok(new EmailAddress(value))
            : Result.Fail($"'{value}' is not a valid email address");

    public static Result<List<EmailAddress>> TryCreateMany(IEnumerable<string> emailAddresses) =>
        emailAddresses
            .Select(TryCreate)
            .Merge()
            .Map(r => r.ToList());

    public static implicit operator string(EmailAddress email) => email.Value;
    public static explicit operator EmailAddress(string email) => TryCreate(email).Value;
    public override string ToString() => Value;

    private static bool IsValidEmail(string email)
    {
        // https://stackoverflow.com/questions/1365407/c-sharp-code-to-validate-email-address
        var trimmedEmail = email.Trim();
        if (trimmedEmail.EndsWith(".")) return false;

        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == trimmedEmail;
        }
        catch
        {
            return false;
        }
    }
}