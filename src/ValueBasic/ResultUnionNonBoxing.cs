using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ResultWithUnion.Common;

namespace ResultWithUnion.ValueBasic;

// Hand-written union: real fields plus HasValue/TryGetValue, so no case is boxed.
// T must not overlap Error or ImmutableArray<Error> (e.g. object) and a null success is read as a failure.
[Union]
public readonly struct ResultUnionNonBoxing<T> : IUnion
{
    private const byte NoneTag = 0;
    private const byte ValueTag = 1;
    private const byte ErrorTag = 2;
    private const byte ErrorsTag = 3;

    private readonly T? _value;
    private readonly ImmutableArray<Error> _errors;
    private readonly byte _tag;

    public ResultUnionNonBoxing(T value)
    {
        if(value is null)
        {
            return;
        }

        _value = value;
        _tag = ValueTag;
    }

    public ResultUnionNonBoxing(Error error)
    {
        _errors = ImmutableArray.Create(error);
        _tag = ErrorTag;
    }

    public ResultUnionNonBoxing(ImmutableArray<Error> errors)
    {
        _errors = errors;
        _tag = ErrorsTag;
    }

    public object? Value => _tag switch
    {
        ValueTag => _value,
        ErrorTag => _errors[0],
        ErrorsTag => _errors,
        _ => null,
    };

    public bool HasValue => _tag != NoneTag;

    public bool TryGetValue(out T value)
    {
        value = _value!;
        return _tag == ValueTag;
    }

    public bool TryGetValue(out Error value)
    {
        value = _tag == ErrorTag ? _errors[0] : default;
        return _tag == ErrorTag;
    }

    public bool TryGetValue(out ImmutableArray<Error> value)
    {
        value = _errors;
        return _tag == ErrorsTag;
    }

    public bool IsSuccess => _tag == ValueTag;

    public T GetValue() => _tag == ValueTag
        ? _value!
        : throw new InvalidOperationException("Value can be retrieved only when the result is not an error");

    public ImmutableArray<Error> Errors => _tag switch
    {
        ErrorTag or ErrorsTag => _errors.IsDefault ? ImmutableArray<Error>.Empty : _errors,
        ValueTag => throw new InvalidOperationException("Errors can be retrieved only when the result is an error"),
        _ => ImmutableArray<Error>.Empty,
    };

    public static ResultUnionNonBoxing<T> From(Error[] errors) => new(ImmutableArray.Create(errors.AsSpan()));

    public static ResultUnionNonBoxing<T> From(List<Error> errors) => new(ImmutableArray.Create(CollectionsMarshal.AsSpan(errors)));

    public static ResultUnionNonBoxing<T> From(IEnumerable<Error> errors) => new(ErrorCollection.Create(errors));

    public static ResultUnionNonBoxing<T> From(params ReadOnlySpan<Error> errors) => new(ImmutableArray.Create(errors));
}
