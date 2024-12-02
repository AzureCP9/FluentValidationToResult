using FluentValidation;

namespace FluentValidationToResult.FluentValidation;
public static class ValidatorExtensions
{
    public static IRuleBuilderOptions<T, TProperty?> SetOptionalValidator<T, TProperty>(
    this IRuleBuilderInitial<T, TProperty?> ruleBuilder,
    IValidator<TProperty> validator) where TProperty : class
        => ruleBuilder
            .SetValidator(validator as IValidator<TProperty?>)
            .When(x => x != null);
}