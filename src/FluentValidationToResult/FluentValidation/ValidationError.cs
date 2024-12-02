using FluentResults;

namespace FluentValidationToResult.FluentValidation;

public class ValidationError(string message) : Error(message);

public class ObjectValidationError : ValidationError
{
    public ObjectValidationErrorData[] Fields { get; init; } = Array.Empty<ObjectValidationErrorData>();

    public ObjectValidationError(ObjectValidationErrorData validationErrorData) : base(validationErrorData.ToString())
    {
        Fields = [validationErrorData];
    }

    public ObjectValidationError(string message, ObjectValidationErrorData validationErrorData) :
        base($"{message} {validationErrorData}")
    {
        Fields = [validationErrorData];
    }

    public ObjectValidationError(string message, IEnumerable<ObjectValidationErrorData> errors) : base(
        $"{message} {{ {string.Join(", ", errors.Select(x => x.ToString()))} }}"
    )
    {
        Fields = errors.ToArray();
    }
    public override string ToString() => Message;
}

public abstract record ObjectValidationErrorData(string Key)
{
    public record Object(
        IEnumerable<ObjectValidationErrorData> Children,
        string Key = "" // Top level object has no key
    ) : ObjectValidationErrorData(Key)
    {
        public override string ToString() => string.IsNullOrWhiteSpace(Key) ?
            $"{{ {string.Join(", ", Children)} }}" :
            $"{Key}: {{ {string.Join(", ", Children)} }}";
    };

    public record Field(string Message, string Key) :
        ObjectValidationErrorData(Key)
    {
        public override string ToString() => $"{Key}: {Message}";
    }

}

public static class ValidationErrorDataExtensions
{
    public static ObjectValidationErrorData Match(
        this ObjectValidationErrorData data,
        Func<ObjectValidationErrorData.Field, ObjectValidationErrorData> fieldFunc,
        Func<ObjectValidationErrorData.Object, ObjectValidationErrorData> objectFunc
    ) => data switch
    {
        ObjectValidationErrorData.Field fieldData => fieldFunc(fieldData),
        ObjectValidationErrorData.Object objectData => objectFunc(objectData),
        _ => throw new InvalidOperationException("Invalid ValidationErrorData type")
    };
}