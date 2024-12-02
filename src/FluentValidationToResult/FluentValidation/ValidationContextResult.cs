using FluentResults;
using FluentValidation;
using FluentValidationToResult.Utils;
using System.Linq.Expressions;

namespace FluentValidationToResult;
public record ValidationContextResult<T>(ValidationContext<T> Context, Result<T> ValidationResult)
{
    public static string GenerateKey<TResult>(string propertyPath) =>
        string.IsNullOrEmpty(propertyPath) ? $"{ExpressionExtensions.ExpressionRoot}:{typeof(TResult).FullName}" : $"{propertyPath}:{typeof(TResult).FullName}";

    public TResult ExpectValue<TResult>(Expression<Func<T, object?>> expression) => ExpectResult<TResult>(expression).Value;

    public Result<TResult> ExpectResult<TResult>(Expression<Func<T, object?>> expression)
    {
        var propertyPathResult = expression.TryGetPropertyValue();
        if (propertyPathResult.IsFailed) return propertyPathResult.ToResult();

        var key = GenerateKey<TResult>(propertyPathResult.Value);

        if (Context.RootContextData.TryGetValue(key, out var resultObj) && resultObj is Result<TResult> result)
            return result;

        return Result.Fail<TResult>($"No result found for key '{propertyPathResult.Value}' and type '{typeof(TResult).Name}'");
    }

    public TResult? ExpectValueOrDefault<TResult>(Expression<Func<T, object?>> expression) =>
        ExpectOptionalResult<TResult>(expression).ValueOrDefault;

    private Result<TResult?> ExpectOptionalResult<TResult>(Expression<Func<T, object?>> expression)
    {
        var propertyPathResult = expression.TryGetPropertyValue();
        if (propertyPathResult.IsFailed)
            return propertyPathResult.ToResult<TResult?>();

        var key = GenerateKey<TResult>(propertyPathResult.Value);

        if (Context.RootContextData.TryGetValue(key, out var resultObj) && resultObj is Result<TResult> result)
            return result.ValueOrDefault;

        return default(TResult?);
    }

    public IEnumerable<TResult> ExpectValues<TResult>(Expression<Func<T, IEnumerable<object?>>> expression) =>
        ExpectResults<TResult>(expression).Value;

    public Result<IEnumerable<TResult>> ExpectResults<TResult>(Expression<Func<T, IEnumerable<object?>>> expression)
    {
        var propertyPathResult = expression.TryGetPropertyValue();
        if (propertyPathResult.IsFailed) return propertyPathResult.ToResult();

        var keyPrefix = GenerateKey<TResult>(propertyPathResult.Value);

        var normalizedKeyPrefix = RemoveIndexFromKey(keyPrefix);

        var results = Context.RootContextData
            .Where(kvp => NormalizeKey(kvp.Key).StartsWith(normalizedKeyPrefix))
            .Select(kvp => kvp.Value)
            .OfType<Result<TResult>>()
            .ToList();

        return results.Count > 0
            ? Result.Merge(results.ToArray())
            : Result.Fail<IEnumerable<TResult>>($"No results found for '{propertyPathResult.Value}' and type '{typeof(TResult).Name}'");
    }

    /// <summary>
    /// Removes index portions (e.g., "[0]", "[1]") from a key for comparison purposes.
    /// </summary>
    /// <param name="key">The key that may contain indices.</param>
    /// <returns>The key with indices removed.</returns>
    private static string RemoveIndexFromKey(string key) =>
        System.Text.RegularExpressions.Regex.Replace(key, @"\[\d+\]", string.Empty);

    private static string NormalizeKey(string key) => RemoveIndexFromKey(key);
}