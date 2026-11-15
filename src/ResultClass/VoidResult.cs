using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace ResultClass;

public sealed class VoidResult
{
    public bool IsSuccess { get; private set; }

    private readonly ImmutableArray<Error> _errors;
    public ImmutableArray<Error> Errors => IsSuccess
        ? throw new InvalidOperationException("Errors can be retrieved only when the result is an error")
        : _errors.IsDefault ? ImmutableArray<Error>.Empty : _errors;

    public VoidResult()
    {
        _errors = default;
        IsSuccess = true;
    }

    public VoidResult(ImmutableArray<Error> errors)
    {
        _errors = errors;
        IsSuccess = false;
    }

    public bool HasValue => !IsSuccess;

    public bool TryGetValue(out ImmutableArray<Error> errors)
    {
        errors = _errors.IsDefault ? ImmutableArray<Error>.Empty : _errors;
        return !IsSuccess;
    }

    public static VoidResult From(Error error) => new(ImmutableArray.Create(error));

    public static VoidResult From(Error[] errors) => new(ImmutableArray.Create(errors.AsSpan()));

    public static VoidResult From(List<Error> errors) => new(ImmutableArray.Create(CollectionsMarshal.AsSpan(errors)));

    public static VoidResult From(IEnumerable<Error> errors) => new(ErrorCollection.Create(errors));

    public static VoidResult From(params ReadOnlySpan<Error> errors) => new(ImmutableArray.Create(errors));

    public static implicit operator VoidResult(Error error) => From(error);

    public static implicit operator VoidResult(Error[] errors) => From(errors);

    public static implicit operator VoidResult(List<Error> errors) => From(errors);

    public static implicit operator VoidResult(ReadOnlySpan<Error> errors) => From(errors);
}
