using System.Collections.Immutable;
using System.Runtime.InteropServices;
using ResultWithUnion.Common;

namespace ResultWithUnion.ValueBasic;

public struct ResultStruct<TValue>
{
    public bool IsSuccess { get; private set; }

    private readonly TValue? _value;
    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Value can be retrieved only when the result is not an error");

    private readonly ImmutableArray<Error> _errors;
    public ImmutableArray<Error> Errors => IsSuccess
        ? throw new InvalidOperationException("Errors can be retrieved only when the result is an error")
        : _errors.IsDefault ? ImmutableArray<Error>.Empty : _errors;

    public ResultStruct(TValue value)
    {
        _value = value;
        _errors = default;
        IsSuccess = true;
    }

    public ResultStruct(ImmutableArray<Error> errors)
    {
        _value = default;
        _errors = errors;
        IsSuccess = false;
    }

    public bool HasValue => IsSuccess || !_errors.IsDefault;

    public bool TryGetValue(out TValue value)
    {
        value = _value!;
        return IsSuccess;
    }

    public bool TryGetValue(out ImmutableArray<Error> errors)
    {
        errors = _errors.IsDefault ? ImmutableArray<Error>.Empty : _errors;
        return !IsSuccess;
    }

    public static ResultStruct<TValue> From(Error error) => new(ImmutableArray.Create(error));

    public static ResultStruct<TValue> From(Error[] errors) => new(ImmutableArray.Create(errors.AsSpan()));

    public static ResultStruct<TValue> From(List<Error> errors) => new(ImmutableArray.Create(CollectionsMarshal.AsSpan(errors)));

    public static ResultStruct<TValue> From(IEnumerable<Error> errors) => new(ErrorCollection.Create(errors));

    public static ResultStruct<TValue> From(params ReadOnlySpan<Error> errors) => new(ImmutableArray.Create(errors));

    public static implicit operator ResultStruct<TValue>(Error error) => From(error);

    public static implicit operator ResultStruct<TValue>(Error[] errors) => From(errors);

    public static implicit operator ResultStruct<TValue>(List<Error> errors) => From(errors);

    public static implicit operator ResultStruct<TValue>(ReadOnlySpan<Error> errors) => From(errors);
}
