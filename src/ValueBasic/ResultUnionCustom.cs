using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ResultWithUnion.Common;

namespace ResultWithUnion.ValueBasic;

// Hand-written union: real fields plus HasValue/TryGetValue, so no case is boxed.
// T must not overlap ImmutableArray<Error> (e.g. object) and a null success is read as a failure.
[Union]
public readonly struct ResultUnionCustom<T> : IUnion
{
    private readonly T? _value;
    private readonly ImmutableArray<Error> _errors;

    public ResultUnionCustom(T value)
    {
        if(value is null)
        {
            return;
        }

        _value = value;
        IsSuccess = true;
    }

    public ResultUnionCustom(ImmutableArray<Error> errors)
        => _errors = errors;

    public readonly bool IsSuccess { get; }

    public object? Value => IsSuccess
        ? _value
        : _errors.IsDefault ? null : _errors;

    public T GetValue() => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Value can be retrieved only when the result is not an error");

    public ImmutableArray<Error> Errors => IsSuccess
        ? throw new InvalidOperationException("Errors can be retrieved only when the result is an error")
        : _errors.IsDefault ? ImmutableArray<Error>.Empty : _errors;

    public static ResultUnionCustom<T> From(Error error) => new(ImmutableArray.Create(error));

    public static ResultUnionCustom<T> From(Error[] errors) => new(ImmutableArray.Create(errors.AsSpan()));

    public static ResultUnionCustom<T> From(List<Error> errors) => new(ImmutableArray.Create(CollectionsMarshal.AsSpan(errors)));

    public static ResultUnionCustom<T> From(IEnumerable<Error> errors) => new(ErrorCollection.Create(errors));

    public static ResultUnionCustom<T> From(params ReadOnlySpan<Error> errors) => new(ImmutableArray.Create(errors));
}
