using AutoFixture;
using FluentAssertions;
using FluentResults;
using FluentValidation;
using FluentValidationToResult.FluentValidation;
using FluentValidationToResult.Samples.DomainDrivenDesign.DomainObjects;
using FluentValidationToResult.Samples.Structs;
using NodaTime;

namespace FluentValidationToResult.UnitTests.Tests;
public class ValidationContextResultTests
{
    public record TestDto1(string PropertyOne, int PropertyTwo, string[] Collection)
    {
        public static Result<TestDto1> TryCreate(string propertyOne, int propertyTwo, string[] collection) =>
            string.IsNullOrWhiteSpace(propertyOne)
                ? Result.Fail<TestDto1>("PropertyOne must not be empty.")
                : Result.Ok(new TestDto1(propertyOne, propertyTwo, collection));
    }

    public record NullableTestDto(string? PropertyOne);
    private readonly Fixture _fixture = new();
    private readonly TestDto1 _testDto;
    private readonly NullableTestDto _nullableTestDto;

    public ValidationContextResultTests()
    {
        _testDto = _fixture.Freeze<TestDto1>();
        _nullableTestDto = _fixture.Freeze<NullableTestDto>();
    }

    [Fact]
    public void ValidateToContextResult_WithValidInput_ShouldReturnSuccessfulResult()
    {
        // Arrange
        var dto = _testDto;
        var validator = new InlineValidator<TestDto1>();
        validator.RuleFor(x => x.PropertyOne).NotEmpty();
        validator.RuleFor(x => x.PropertyTwo).GreaterThan(0);

        // Act
        var validationContextResult = validator.ValidateToContextResult(dto);

        // Assert
        validationContextResult.ValidationResult.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ValidateToContextResult_WithInvalidInput_ShouldReturnValidationErrors()
    {
        // Arrange
        var invalidDto = _testDto with { PropertyOne = string.Empty };
        var validator = new InlineValidator<TestDto1>();
        validator.RuleFor(x => x.PropertyOne).NotEmpty();
        validator.RuleFor(x => x.PropertyTwo).GreaterThan(0);

        // Act
        var validationContextResult = validator.ValidateToContextResult(invalidDto);

        // Assert
        validationContextResult.ValidationResult.IsFailed.Should().BeTrue();
        var validationError = validationContextResult.ValidationResult.Errors
            .First()
            .Should()
            .BeOfType<ObjectValidationError>().Subject;
        validationError.Fields.Should().ContainSingle(field => field.Key == "PropertyOne");
    }

    [Fact]
    public void ExpectResult_WithValidEnsureResultCall_ShouldReturnExpectedResult()
    {
        // Arrange
        var dto = _testDto;
        var validator = new InlineValidator<TestDto1>();
        validator.RuleFor(x => x.PropertyOne).EnsureResult(NonEmptyString.TryCreate);

        var validationContextResult = validator.ValidateToContextResult(dto);

        // Act
        var result = validationContextResult.ExpectResult<NonEmptyString>(x => x.PropertyOne);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(NonEmptyString.TryCreate(dto.PropertyOne).Value);
    }

    [Fact]
    public void ExpectResult_WithInvalidProperty_ShouldReturnFailure()
    {
        // Arrange
        var dto = _testDto with { PropertyOne = string.Empty };
        var validator = new InlineValidator<TestDto1>();
        validator.RuleFor(x => x.PropertyOne).EnsureResult(NonEmptyString.TryCreate);

        var validationContextResult = validator.ValidateToContextResult(dto);

        // Act
        var result = validationContextResult.ExpectResult<NonEmptyString>(x => x.PropertyOne);

        // Assert
        validationContextResult.ValidationResult.IsFailed.Should().BeTrue();
        validationContextResult.ValidationResult.Errors
            .Should()
            .ContainSingle(e => e.Message == "TestDto1 { PropertyOne: NonEmptyString { Value: must not be empty } }");
        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public void ExpectResults_WithEnsureResultCallOnSequence_ShouldReturnExpectedResults()
    {
        // Arrange
        var dto = _testDto with
        {
            Collection = ["example@example.com"]
        };

        var validator = new InlineValidator<TestDto1>();
        validator.RuleForEach(x => x.Collection).EnsureResult(EmailAddress.TryCreate);

        var validationContextResult = validator.ValidateToContextResult(dto);

        // Act
        var result = validationContextResult.ExpectResults<EmailAddress>(x => x.Collection);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(dto.Collection.Select(x => EmailAddress.TryCreate(x).Value));
    }

    [Fact]
    public void ExpectResults_WithInvalidCollection_ShouldReturnFailure()
    {
        // Arrange
        var dto = _testDto with { Collection = Array.Empty<string>() };

        var validator = new InlineValidator<TestDto1>();
        validator.RuleFor(x => x.Collection).NotEmpty();
        validator.RuleForEach(x => x.Collection).EnsureResult(NonEmptyString.TryCreate);

        var validationContextResult = validator.ValidateToContextResult(dto);

        // Act
        var result = validationContextResult.ExpectResults<NonEmptyString>(x => x.Collection);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message.Contains("No results found for"));
    }

    [Fact]
    public void EnsureOptionalResult_WithNullValue_ShouldReturnSuccess()
    {
        // Arrange
        var dto = _nullableTestDto with { PropertyOne = default };
        var validator = new InlineValidator<NullableTestDto>();
        validator.RuleFor(x => x.PropertyOne).EnsureOptionalResult(NonEmptyString.TryCreate);

        var validationContextResult = validator.ValidateToContextResult(dto);

        // Act
        var result = validationContextResult.ExpectResult<NonEmptyString?>(x => x.PropertyOne);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public void EnsureOptionalResult_WithNonEmptyValue_ShouldReturnExpectedResult()
    {
        // Arrange
        var dto = _nullableTestDto with { PropertyOne = "ValidValue" };
        var validator = new InlineValidator<NullableTestDto>();
        validator.RuleFor(x => x.PropertyOne).EnsureOptionalResult(NonEmptyString.TryCreate);

        var validationContextResult = validator.ValidateToContextResult(dto);

        // Act
        var result = validationContextResult.ExpectResult<NonEmptyString>(x => x.PropertyOne);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(NonEmptyString.TryCreate(dto.PropertyOne).Value);
    }

    [Fact]
    public void EnsureOptionalResult_WithInvalidValue_ShouldReturnFailure()
    {
        // Arrange
        var dto = _nullableTestDto with { PropertyOne = string.Empty };
        var validator = new InlineValidator<NullableTestDto>();
        validator.RuleFor(x => x.PropertyOne).EnsureOptionalResult(NonEmptyString.TryCreate);

        var validationContextResult = validator.ValidateToContextResult(dto);

        // Act
        var result = validationContextResult.ExpectResult<NonEmptyString?>(x => x.PropertyOne);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message.Contains("must not be empty"));
    }

    [Fact]
    public void EnsureOptionalResult_WithNullValue_ShouldSucceedWithNullForBothNullableAndNonNullableExpectations()
    {
        // Arrange
        var dto = _nullableTestDto with { PropertyOne = null };
        var validator = new InlineValidator<NullableTestDto>();
        validator.RuleFor(x => x.PropertyOne).EnsureOptionalResult(NonEmptyString.TryCreate);

        var validationContextResult = validator.ValidateToContextResult(dto);

        // Act
        var result1 = validationContextResult.ExpectResult<NonEmptyString?>(x => x.PropertyOne);
        var result2 = validationContextResult.ExpectResult<NonEmptyString>(x => x.PropertyOne);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result1.Value.Should().BeNull();
        result2.IsSuccess.Should().BeTrue();
        result2.Value.Should().BeNull();
    }

    [Fact]
    public void ValidateToContextResult_WithValidEnsureResultForRootObject_ShouldReturnSuccess_WithEquivalentExpressions()
    {
        // Arrange
        var dto = _testDto;

        var validator = new InlineValidator<TestDto1>();
        validator.RuleFor(x => x).EnsureResult(x => TestDto1.TryCreate(x.PropertyOne, x.PropertyTwo, x.Collection));

        // Act
        var validationContextResult = validator.ValidateToContextResult(dto);
        var result = validationContextResult.ExpectResult<TestDto1>(y => y);

        // Assert
        validationContextResult.ValidationResult.IsSuccess.Should().BeTrue();
        validationContextResult.ValidationResult.Value.Should().BeEquivalentTo(dto);
        result.Value.Should().BeEquivalentTo(dto);
    }

    [Fact]
    public void ExpectValueAndExpectValueOrDefault_WithNullableStruct_ShouldWorkForNullableT_AndThrowOrDefaultForNonNullableT()
    {
        // Arrange
        var dto = new NullableTestDto("2024-12-01");
        var validator = new InlineValidator<NullableTestDto>();
        validator.RuleFor(x => x.PropertyOne).EnsureOptionalResult(x => x.TryToNullableLocalDate());

        var validationContextResult = validator.ValidateToContextResult(dto);

        // Act
        var resultUsingNullableExpectValue = validationContextResult.ExpectValue<LocalDate?>(x => x.PropertyOne);
        var resultUsingNullableExpectValueOrDefault = validationContextResult.ExpectValueOrDefault<LocalDate?>(x => x.PropertyOne);
        var incorrectResultUsageFromDefaultStruct = validationContextResult.ExpectValueOrDefault<LocalDate>(x => x.PropertyOne);

        // Assert
        resultUsingNullableExpectValue.Should().NotBeNull();
        resultUsingNullableExpectValue.Should().Be(LocalDate.FromDateTime(DateTime.Parse("2024-12-01")));

        resultUsingNullableExpectValueOrDefault.Should().NotBeNull();
        resultUsingNullableExpectValueOrDefault.Should().Be(LocalDate.FromDateTime(DateTime.Parse("2024-12-01")));

        incorrectResultUsageFromDefaultStruct.Should().NotBeNull();
        incorrectResultUsageFromDefaultStruct.Should().Be(LocalDate.FromDateTime(DateTime.Parse("0001-01-01")));

        Action incorrectExpectValueCall = () => validationContextResult.ExpectValue<LocalDate>(x => x.PropertyOne);
            incorrectExpectValueCall.Should().Throw<InvalidOperationException>();
    }
}