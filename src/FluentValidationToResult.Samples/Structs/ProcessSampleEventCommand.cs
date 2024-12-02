using NodaTime;

namespace FluentValidationToResult.Samples.Structs;
public record ProcessSampleEventCommand(
    LocalDate? NullableDate
);