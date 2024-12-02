using FluentAssertions;
using FluentValidationToResult.Utils;
using System.Linq.Expressions;

namespace FluentValidationToResult.UnitTests.Tests;

public class ExpressionExtensionsTests
{

    private record TestRecord(string PropertyOne, TestNestedRecord Nested);
    private record TestNullIntermediateRecord(string PropertyOne, TestNestedRecord? Nested);
    private record TestNestedRecord(int PropertyTwo);

    private readonly TestRecord _testRecord = new("Value1", new(42));
    private readonly TestNullIntermediateRecord _testNullIntermediateRecord = new("Value1", null);

    [Fact]
    public void TryGetPropertyValue_WithSimpleProperty_ShouldReturnPropertyPath()
    {
        // Arrange
        Expression<Func<TestRecord, string>> expression = x => x.PropertyOne;

        // Act
        var result = expression.TryGetPropertyValue();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("PropertyOne");
    }

    [Fact]
    public void TryGetPropertyValue_WithNestedProperty_ShouldReturnPropertyPath()
    {
        // Arrange
        Expression<Func<TestRecord, int>> expression = x => x.Nested.PropertyTwo;

        // Act
        var result = expression.TryGetPropertyValue();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("Nested.PropertyTwo");
    }

    [Fact]
    public void TryGetPropertyValue_WithRootExpression_ShouldReturnRoot()
    {
        // Arrange
        Expression<Func<TestRecord, TestRecord>> expression = x => x;

        // Act
        var result = expression.TryGetPropertyValue();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(ExpressionExtensions.ExpressionRoot);
    }


    [Fact]
    public void TryGetPropertyValue_WithUnaryExpression_ShouldReturnPropertyPath()
    {
        // Arrange
        Expression<Func<TestRecord, int>> expression = x => +x.Nested.PropertyTwo;

        // Act
        var result = expression.TryGetPropertyValue();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("Nested.PropertyTwo");
    }

    [Fact]
    public void TryGetPropertyValue_WithMethodCallExpression_ShouldReturnFailure()
    {
        // Arrange
        Expression<Func<TestRecord, object>> expression = x => x.Nested.ToString();

        // Act
        var result = expression.TryGetPropertyValue();

        // Assert
        result.IsFailed.Should().BeTrue();
    }
}
