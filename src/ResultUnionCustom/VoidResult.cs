using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ResultUnionCustom;

// Hand-written union: real fields plus HasValue/TryGetValue, so no case is boxed. default is a success.
[Union]
public readonly struct VoidResult : IUnion
{
    // default(ImmutableArray) means "no errors set", so default(VoidResult) is a success.
    private readonly ImmutableArray<Error> _errors;

    public VoidResult(ImmutableArray<Error> errors)
        => _errors = errors.IsDefault ? ImmutableArray<Error>.Empty : errors;

    public object? Value => IsSuccess ? null : _errors;

    public bool HasValue => !IsSuccess;

    public bool TryGetValue(out ImmutableArray<Error> value)
    {
        value = _errors;
        return !IsSuccess;
    }

    public bool IsSuccess => _errors.IsDefault;

    public ImmutableArray<Error> Errors => IsSuccess
        ? throw new InvalidOperationException("Errors can be retrieved only when the result is an error")
        : _errors;

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
