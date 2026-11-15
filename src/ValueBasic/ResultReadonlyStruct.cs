using System.Collections.Immutable;
using System.Runtime.InteropServices;
using ResultWithUnion.Common;

namespace ResultWithUnion.ValueBasic;

public readonly struct ResultReadonlyStruct<TValue>
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

    public ResultReadonlyStruct(TValue value)
    {
        _value = value;
        _errors = default;
        IsSuccess = true;
    }

    public ResultReadonlyStruct(ImmutableArray<Error> errors)
    {
        _value = default;
        _errors = errors;
        IsSuccess = false;
    }

    public static ResultReadonlyStruct<TValue> From(Error error) => new(ImmutableArray.Create(error));

    public static ResultReadonlyStruct<TValue> From(Error[] errors) => new(ImmutableArray.Create(errors.AsSpan()));

    public static ResultReadonlyStruct<TValue> From(List<Error> errors) => new(ImmutableArray.Create(CollectionsMarshal.AsSpan(errors)));

    public static ResultReadonlyStruct<TValue> From(IEnumerable<Error> errors) => new(ErrorCollection.Create(errors));

    public static ResultReadonlyStruct<TValue> From(params ReadOnlySpan<Error> errors) => new(ImmutableArray.Create(errors));
}
