using FluentResults;
using System.Linq.Expressions;

namespace FluentValidationToResult.Utils;
public static class ExpressionExtensions
{
    public const string ExpressionRoot = "Root";

    /// <summary>
    /// Tries to get the property value from the given expression.
    /// </summary>
    /// <typeparam name="T">The type of the object being validated.</typeparam>
    /// <typeparam name="TResult">The expected result type for the expression.</typeparam>
    /// <param name="self">The expression specifying the property.</param>
    /// <returns>A Result containing the property path as a string if successful, otherwise a failure.</returns>
    public static Result<string> TryGetPropertyValue<T, TResult>(
        this Expression<Func<T, TResult>> self)
    {
        if (self.Body is ParameterExpression pe && pe.Name is not null) // handle root-only expression (e.g. "Foo => Foo")
            return ExpressionRoot;

        return self.Body.TryExtractMemberExpressionPropertyPath();
    }

    /// <summary>
    /// Traverses an expression tree to get the property path.
    /// Works only with simple member expressions such as <c>Foo.Bar</c> or <c>Foo.Bar.Foo</c>. MethodCallExpressions are disallowed and will return an empty string.
    /// </summary>
    /// <param name="self">The expression to traverse.</param>
    /// <returns>The property path as a string if successful, otherwise an empty string.</returns>
    private static Result<string> TryExtractMemberExpressionPropertyPath(this Expression expression)
    {
        var type = expression.GetType();
        return expression switch
        {
            // +Foo.Bar, !Foo.Bar, etc
            UnaryExpression unaryExpression when unaryExpression.Operand is MemberExpression memberExpression =>
                TryExtractMemberExpressionPropertyPath(memberExpression),
            UnaryExpression unaryExpression when unaryExpression.Operand is ParameterExpression parameterExpression =>
                TryExtractMemberExpressionPropertyPath(parameterExpression),
            // Foo.Bar, Foo.Bar.Foo, etc
            MemberExpression memberExpr when memberExpr.Expression is not null => memberExpr.Expression.TryExtractMemberExpressionPropertyPath()
                .Map(parentPath => string.IsNullOrEmpty(parentPath)
                    ? memberExpr.Member.Name
                    : $"{parentPath}.{memberExpr.Member.Name}"),
            ParameterExpression => Result.Ok(string.Empty),
            // Fail on method calls such as Foo.ToString()
            MethodCallExpression => Result.Fail<string>("Unsupported method call expression in member path"),
            _ => Result.Fail<string>("Unsupported expression type")
        };
    }

    /// <summary>
    /// Combines two expressions into a single expression.
    /// </summary>
    /// <typeparam name="TInput"></typeparam>
    /// <typeparam name="TIntermediate"></typeparam>
    /// <param name="self"></param>
    /// <param name="next"></param>
    /// <returns></returns>
}
