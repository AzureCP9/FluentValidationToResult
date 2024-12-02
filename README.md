# **FluentValidationToResult**

FluentValidationToResult is a utility library that bridges the gap between [FluentValidation](https://fluentvalidation.net/) and [FluentResults](https://github.com/altmann/FluentResults), enabling seamless validation workflows with rich result objects. It simplifies the process of converting validation results into actionable and structured responses, especially useful in domain-driven design (DDD) and service-oriented architectures.

---

## **Features**

- **Integration with FluentValidation**: Extends FluentValidation rules to work directly with FluentResults.
- **Validation Context Result**: Exposes a `ValidationContextResult` type that encapsulates validation errors for a specific type and offers methods to retrieve them as `Result` or `Result<T>`.
- **Optional Validator**: Provides setting an optional validator for convenience when a property is nullable (validate if not null)

---

## Table of Contents
- [Features](#features)
- [Installation](#installation)
- [Usage](#usage)
    - [1. Handling Nullable and Non-Nullable Properties](#1-handling-nullable-and-non-nullable-properties)
    - [2. Handling Nullable Structs](#2-handling-nullable-structs)
    - [3. ExpectValueOrDefault](#3-expectvalueordefault)
    - [4. SetOptionalValidator](#4-setoptionalvalidator)
    - [5. Samples](#5-samples)
- [Contributing](#contributing)

## **Installation**

Add the package to your project via NuGet:

```sh
dotnet add package FluentValidationToResult
```

## **Usage**

### **1. Handling Nullable and Non-Nullable Properties**

This example demonstrates how to validate a DTO containing a nullable and a non-nullable property and map it into a domain command.

#### **DTO**
```csharp
public record UpdateUserRequestDto(
    string Email
    string? Address,
);
```

#### **Domain Command**
```csharp
public record UpdateUserRequest(
    Email Email
    Address? Address,
);
```

#### **Validating and Mapping to Command**
```csharp
public static class UpdateUserRequestDtoExtensions
{
    public static Result<UpdateUserCommand> TryToCommand(this UpdateUserRequestDto self)
    {
        // Define validation rules
        var validator = new InlineValidator<UpdateUserRequestDto>();
        // Email.TryCreate would return Result<Email>, the strongly typed model
        validator.RuleFor(x => x.Email).EnsureResult(x => Email.TryCreate(x));
        // Address.TryCreate => Result<Address>
        validator.RuleFor(x => x.Address).EnsureOptionalResult(x => Address.TryCreate(x))

        // Validate the DTO
        var validationContextResult = validator.ValidateToContextResult(self);

        // Use ValidationContextResult methods to extract results
        return validationContextResult.ValidationResult
            .Bind(_ => Result.Ok(new UpdateUserCommand(
                Nickname: validationContextResult.ExpectValue<Nickname>(x => x.Nickname),
                Email: validationContextResult.ExpectValue<Address?>(x => x.Address)
            )));
    }
}
```

### **2. Handling Nullable Structs**

Always ensure validators return nullable structs (T?) when optional values are required. Using `EnsureOptionalResult(x => NonNullableFactoryMethod(x))` and later calling `ExpectValue<SomeStruct?>(x => x.Prop)` will throw an `InvalidOperationException`. The compiler won’t enforce nullability, so align types properly to avoid runtime issues.

#### **Correct Usage**
```csharp
// Using NodaTime LocalDate struct as an example
validator.RuleFor(x => x.PropertyOne)
    .EnsureOptionalResult(x => TryToNullableLocalDate(x)); // Returns `Result<LocalDate?>`
    
var result = validationContextResult.ExpectValue<LocalDate?>(x => x.PropertyOne); 
```

### **3. ExpectValueOrDefault**

The `ValidationContextResult` also exposes a method called `ExpectValueOrDefault`. The use of this method is generally discouraged since it may hide validation errors. As the name implies, it either returns the value expected or the `default` for type `T` (once again, this will not be null for value types) so beware. This tends to be used in scenarios where the property is optional or nullable, and the absence of a value is acceptable or expected, such as handling default values for optional fields or when a missing value is not considered a validation failure. See below example in [section 4](#4-setoptionalvalidator) for an idea of when to use it.

### **4. SetOptionalValidator**

`SetOptionalValidator` is a helper for applying validators to nullable properties. It ensures that the inner validation only runs if the property is not null.

#### **Usage**
```csharp
var validator = new InlineValidator<RequestDto>();

validator.RuleFor(x => x.TypeToValidate).SetOptionalValidator(new SomeCustomIValidator())

var validationContextResult = validator.ValidateToContextResult(self);

return validationContextResult.ValidationResult
    .Bind(_ => Result.Ok(new RequestCommand(
      // We must call ExpectValueOrDefault in case the property is null and no validation occurs
        TypeToValidate: validationContextResult.ExpectValueOrDefault<TypeToValidate>(x => x.TypeToValidate),
    )));

```

### **5. Samples**

Please refer to `src/FluentValidationResult.Samples` for some examples on general usage.

## **Contributing**

This is currently a personal project of mine but if requests come in for contribution and wanting to expand it, it will be considered!