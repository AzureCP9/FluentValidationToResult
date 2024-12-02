using FluentResults;
using FluentValidation;
using FluentValidation.Results;

namespace FluentValidationToResult.FluentValidation;

public static class FluentValidationExtensions
{
    /// <summary>
    /// Converts a list of <see cref="ValidationFailure"/> objects into a <see cref="Result{T}"/> containing an <see cref="ObjectValidationError"/>.
    /// </summary>
    /// <typeparam name="T">The type of the instance being validated.</typeparam>
    /// <param name="errors">The collection of validation errors.</param>
    /// <returns>A failed <see cref="Result{T}"/> containing an <see cref="ObjectValidationError"/> with the validation details.</returns>
    private static Result<T> ToObjectValidationError<T>(this IEnumerable<ValidationFailure> errors)
        where T : notnull =>
        Result.Fail<T>(
            new ObjectValidationError(
                typeof(T).Name,
                errors
                    .GroupBy(e => string.IsNullOrEmpty(e.PropertyName) ? typeof(T).Name : GetRootPropertyName(e.PropertyName))
                    .Select(group => CreateValidationErrorData(group.Key, group))
            )
        );

    /// <summary>
    /// Extracts the root property name from a full property path (e.g., "Foo.Bar" => "Foo").
    /// </summary>
    private static string GetRootPropertyName(string propertyName) =>
        !string.IsNullOrEmpty(propertyName)
            ? propertyName.Split(new[] { '.', '[' }, StringSplitOptions.RemoveEmptyEntries).First()
            : throw new ArgumentNullException(propertyName); // this should be unreachable but might as well be explicit

    /// <summary>
    /// Creates an ObjectValidationErrorData entry, either a field or a nested object.
    /// </summary>
    private static ObjectValidationErrorData CreateValidationErrorData(
        string key,
        IEnumerable<ValidationFailure> errors) =>
        errors.ToList() switch
        {
        [var singleError] when !singleError.PropertyName.Contains('.') => new ObjectValidationErrorData.Field(singleError.ErrorMessage, key),
            _ => new ObjectValidationErrorData.Object(
                errors.Select(e => new ObjectValidationErrorData.Field(e.ErrorMessage, GetRelativePropertyName(e.PropertyName, key))),
                key)
        };

    /// <summary>
    /// Extracts the relative property name from a full property path, relative to a given parent key. (e.g., "Foo.Bar" => "Bar").
    /// </summary>
    /// <param name="propertyPath">The full property path (e.g., "Customer.Address.City").</param>
    /// <param name="parentKey">The parent key to determine the relative position (e.g., "Customer").</param>
    /// <returns>The relative property name with the parent key removed, if applicable.</returns>
    private static string GetRelativePropertyName(string propertyPath, string parentKey) =>
        propertyPath.StartsWith(parentKey + ".") ? propertyPath.Substring(parentKey.Length + 1) : propertyPath;

    /// <summary>
    /// Validates the given instance and returns a Result object.
    /// </summary>
    /// <typeparam name="T">The type of the instance to validate.</typeparam>
    /// <param name="self">The validator instance.</param>
    /// <param name="instance">The object to validate.</param>
    /// <returns>A Result containing the validated object or a validation failure.</returns>
    public static Result<T> ValidateToResult<T>(this AbstractValidator<T> self, T instance) where T : notnull =>
        self.Validate(instance) switch
        {
            { IsValid: true } => instance,
            { Errors: var errors } => errors.ToObjectValidationError<T>()
        };

    /// <summary>
    /// Adds a custom validation rule to ensure that a result is valid using a specified validation function.
    /// </summary>
    /// <typeparam name="T">The type of the object being validated.</typeparam>
    /// <typeparam name="TProp">The type of the property being validated.</typeparam>
    /// <typeparam name="TResult">The result type returned by the validation function.</typeparam>
    /// <param name="ruleBuilder">The rule builder for the property being validated.</param>
    /// <param name="validationFunc">A function that returns a Result based on the property value.</param>
    /// <returns>An updated rule builder with the added custom validation.</returns>
    public static IRuleBuilderOptionsConditions<T, TProp> EnsureResult<T, TProp, TResult>(
       this IRuleBuilder<T, TProp> ruleBuilder,
       Func<TProp, Result<TResult>> validationFunc) =>
        ruleBuilder.Custom((value, context) =>
        {
            var resultKey = ValidationContextResult<T>.GenerateKey<TResult>(context.PropertyPath);

            if (context.RootContextData.ContainsKey(resultKey))
                throw new InvalidOperationException($"EnsureResult has already been called for '{resultKey}'. Only one call is allowed");

            var validationResult = validationFunc(value);
            context.RootContextData[resultKey] = validationResult;
            validationResult.Errors.ForEach(e => context.AddFailure(new ValidationFailure(context.PropertyPath, e.Message)));
        });


    /// <summary>
    /// Adds a custom validation rule to ensure that a result is valid using a specified validation function.
    /// </summary>
    /// <typeparam name="T">The type of the object being validated.</typeparam>
    /// <typeparam name="TProp?">The type of the optional property being validated.</typeparam>
    /// <typeparam name="TResult">The result type returned by the validation function.</typeparam>
    /// <param name="ruleBuilder">The rule builder for the property being validated.</param>
    /// <param name="validationFunc">A function that returns a Result based on the property value.</param>
    /// <returns>An updated rule builder with the added custom validation.</returns>
    public static IRuleBuilderOptionsConditions<T, TProp?> EnsureOptionalResult<T, TProp, TResult>(
     this IRuleBuilder<T, TProp?> ruleBuilder,
     Func<TProp, Result<TResult>> validationFunc) =>
         ruleBuilder.Custom((value, context) =>
         {
             var resultKey = ValidationContextResult<T>.GenerateKey<TResult>(context.PropertyPath);

             if (context.RootContextData.ContainsKey(resultKey))
                 throw new InvalidOperationException($"EnsureResult has already been called for '{resultKey}'. Only one call is allowed");

             Result<TResult?> validationResult = value is not null
                 ? validationFunc(value).Map(r => (TResult?)r)
                 : Result.Ok<TResult?>(default);

             context.RootContextData[resultKey] = validationResult;

             validationResult.Errors.ForEach(e =>
                 context.AddFailure(new ValidationFailure(context.PropertyPath, e.Message)));
         });


    /// <summary>
    /// Converts a ValidationResult to a Result object using the specified validation context.
    /// </summary>
    /// <typeparam name="T">The type of the validated instance.</typeparam>
    /// <param name="validationResult">The result of the validation process.</param>
    /// <param name="validationContext">The validation context used during validation.</param>
    /// <returns>A Result containing the validated object or a validation failure.</returns>
    public static Result<T> ValidateToResult<T>(
      this ValidationResult validationResult,
      ValidationContext<T> validationContext) where T : notnull
    {
        return validationResult switch
        {
            { IsValid: true } => Result.Ok(validationContext.InstanceToValidate),
            { Errors: var errors } => errors.ToObjectValidationError<T>()
        };
    }

    /// <summary>
    /// Validates the given instance using the specified validator and returns a ValidationContextResult.
    /// </summary>
    /// <typeparam name="T">The type of the instance being validated.</typeparam>
    /// <param name="validator">The validator used to validate the instance.</param>
    /// <param name="instance">The object instance to be validated.</param>
    /// <returns>
    /// A <see cref="ValidationContextResult{T}"/> containing the validation context and the result of the validation process.
    /// </returns>
    public static ValidationContextResult<T> ValidateToContextResult<T>(
    this IValidator<T> validator,
    T instance) where T : notnull
    {
        var validationResult = validator.ValidateWithContext(instance, out var validationContext);
        var result = validationResult.ValidateToResult(validationContext);
        return new ValidationContextResult<T>(validationContext, result);
    }

    private static ValidationResult ValidateWithContext<T>(
         this IValidator<T> validator,
         T instance,
         out ValidationContext<T> validationContext)
    {
        validationContext = new ValidationContext<T>(instance);
        return validator.Validate(validationContext);
    }
}