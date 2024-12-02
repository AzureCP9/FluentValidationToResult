
using FluentResults;
using PhoneNumbers;
using PhoneNumber = FluentValidationToResult.Samples.DomainDrivenDesign.DomainObjects.PhoneNumber;

public record MobilePhoneNumber
{
    private PhoneNumber _phoneNumber { get; }
    public string Value => _phoneNumber.Value;

    private MobilePhoneNumber(PhoneNumber phoneNumber) => _phoneNumber = phoneNumber;

    public static Result<MobilePhoneNumber> TryCreate(string value) =>
        PhoneNumber
            .TryCreate(value)
            .Bind(TryCreate);

    private static Result<MobilePhoneNumber> TryCreate(PhoneNumber phoneNumber) =>
        PhoneNumberUtil.GetInstance().GetNumberType(phoneNumber.PhoneNumberData) != PhoneNumberType.MOBILE
            ? Result.Fail($"Invalid mobile phone number: {phoneNumber.Value}")
            : new MobilePhoneNumber(phoneNumber);

    public static implicit operator string(MobilePhoneNumber phoneNumber) => phoneNumber.Value;
    public static implicit operator PhoneNumber(MobilePhoneNumber phoneNumber) => phoneNumber._phoneNumber;
    public static explicit operator MobilePhoneNumber(string phoneNumber) => TryCreate(phoneNumber).Value;

    public override string ToString() => Value;
}