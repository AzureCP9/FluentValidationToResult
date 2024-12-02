using FluentResults;
using NodaTime;
using NodaTime.Text;

namespace FluentValidationToResult.Samples.Structs;
public static class NodaTimeExtensions
{
    public static Result<LocalDate?> TryToNullableLocalDate(this string? dateString)
    {
        if (dateString is null) return Result.Ok<LocalDate?>(null);
        var parseResult = LocalDatePattern.Iso.Parse(dateString);
        return parseResult.Success ? Result.Ok<LocalDate?>(parseResult.Value) : Result.Fail("Invalid ISO date string");
    }

    public static Result<LocalDate> MethodThatReturnsNonNullableStruct(this string? dateString)
    {
        if (dateString is null) return Result.Ok<LocalDate>(default);
        var parseResult = LocalDatePattern.Iso.Parse(dateString);
        return parseResult.Success ? Result.Ok<LocalDate>(parseResult.Value) : Result.Fail("Invalid ISO date string");
    }
}
