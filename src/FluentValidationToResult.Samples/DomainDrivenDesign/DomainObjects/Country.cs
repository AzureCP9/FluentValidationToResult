using FluentResults;
using System.Globalization;

namespace FluentValidationToResult.Samples.DomainDrivenDesign.DomainObjects;

public record Country
{
    public const string BritishCountryCode = "GB";
    public static Country Britain => new(BritishCountryCode);

    public string Value { get; }

    private Country(string value) => Value = value;

    public static Result<Country> TryCreate(string iso3166CountryCode) =>
        IsValidCountryCode(iso3166CountryCode)
            ? new Country(iso3166CountryCode.ToUpperInvariant())
            : Result.Fail($"Invalid ISO3166 country code: {iso3166CountryCode}");

    public static implicit operator string(Country country) => country.Value;
    public static explicit operator Country(string countryCode) => TryCreate(countryCode).Value;
    public override string ToString() => Value;

    public static bool IsValidCountryCode(string countryCode) => CultureInfo
        .GetCultures(CultureTypes.SpecificCultures)
        .Select(culture => new RegionInfo(culture.Name))
        .Any(region => region.TwoLetterISORegionName.Equals(countryCode, StringComparison.OrdinalIgnoreCase));
}