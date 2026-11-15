using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace ResultReadonlyStruct;

public readonly struct ValueResult<TValue>
{
    public readonly bool IsSuccess;

    private readonly TValue? _value;
    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Value can be retrieved only when the result is not an error");

    private readonly ImmutableArray<Error> _errors;
    public ImmutableArray<Error> Errors => IsSuccess
        ? throw new InvalidOperationException("Errors can be retrieved only when the result is an error")
        : _errors.IsDefault ? ImmutableArray<Error>.Empty : _errors;

    public ValueResult(TValue value)
    {
        _value = value;
        _errors = default;
        IsSuccess = true;
    }

    public ValueResult(ImmutableArray<Error> errors)
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

    public static ValueResult<TValue> From(Error error) => new(ImmutableArray.Create(error));

    public static ValueResult<TValue> From(Error[] errors) => new(ImmutableArray.Create(errors.AsSpan()));

    public static ValueResult<TValue> From(List<Error> errors) => new(ImmutableArray.Create(CollectionsMarshal.AsSpan(errors)));

    public static ValueResult<TValue> From(IEnumerable<Error> errors) => new(ErrorCollection.Create(errors));

    public static ValueResult<TValue> From(params ReadOnlySpan<Error> errors) => new(ImmutableArray.Create(errors));

    public static implicit operator ValueResult<TValue>(Error error) => From(error);

    public static implicit operator ValueResult<TValue>(Error[] errors) => From(errors);

    public static implicit operator ValueResult<TValue>(List<Error> errors) => From(errors);

    public static implicit operator ValueResult<TValue>(ReadOnlySpan<Error> errors) => From(errors);
}
