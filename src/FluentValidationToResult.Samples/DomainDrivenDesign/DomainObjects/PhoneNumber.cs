using FluentResults;
using PhoneNumbers;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Metrics;
using System.Text.RegularExpressions;

namespace FluentValidationToResult.Samples.DomainDrivenDesign.DomainObjects;
public record PhoneNumber
{
    private PhoneNumberUtil _phoneNumberUtil { get; } = PhoneNumberUtil.GetInstance();
    public PhoneNumbers.PhoneNumber PhoneNumberData { get; }
    private PhoneNumber(PhoneNumbers.PhoneNumber value) => PhoneNumberData = value;
    public string Value => _phoneNumberUtil.Format(PhoneNumberData, PhoneNumberFormat.E164);

    public static Result<PhoneNumber> TryCreate(string value)
    {
        var invalidPhoneNumberText = $"Invalid phone number: '{value}'";

        var phoneNumberUtil = PhoneNumberUtil.GetInstance();
        try
        {
            var phoneNumber = phoneNumberUtil.Parse(value, Country.BritishCountryCode);

            if (phoneNumberUtil.IsValidNumber(phoneNumber) || Regex.IsMatch(value, @"[a-zA-Z]"))
                return Result.Fail(invalidPhoneNumberText);

            return new PhoneNumber(phoneNumber);
        }
        catch (NumberParseException)
        {
            return Result.Fail(invalidPhoneNumberText);
        }
    }

    public static implicit operator string(PhoneNumber phoneNumber) => phoneNumber.Value;
    public static explicit operator PhoneNumber(string phoneNumber) => TryCreate(phoneNumber).Value;
    public override string ToString() => Value;
}

