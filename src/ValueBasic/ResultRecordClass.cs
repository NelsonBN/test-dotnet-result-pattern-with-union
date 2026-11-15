using System.Collections.Immutable;
using System.Runtime.InteropServices;
using ResultWithUnion.Common;

namespace ResultWithUnion.ValueBasic;

public sealed record class ResultRecordClass<TValue>
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

    public ResultRecordClass(TValue value)
    {
        _value = value;
        _errors = default;
        IsSuccess = true;
    }

    public ResultRecordClass(ImmutableArray<Error> errors)
    {
        _value = default;
        _errors = errors;
        IsSuccess = false;
    }

    public static ResultRecordClass<TValue> From(Error error) => new(ImmutableArray.Create(error));

    public static ResultRecordClass<TValue> From(Error[] errors) => new(ImmutableArray.Create(errors.AsSpan()));

    public static ResultRecordClass<TValue> From(List<Error> errors) => new(ImmutableArray.Create(CollectionsMarshal.AsSpan(errors)));

    public static ResultRecordClass<TValue> From(IEnumerable<Error> errors) => new(ErrorCollection.Create(errors));

    public static ResultRecordClass<TValue> From(params ReadOnlySpan<Error> errors) => new(ImmutableArray.Create(errors));

    public static implicit operator ResultRecordClass<TValue>(Error error) => From(error);

    public static implicit operator ResultRecordClass<TValue>(Error[] errors) => From(errors);

    public static implicit operator ResultRecordClass<TValue>(List<Error> errors) => From(errors);

    public static implicit operator ResultRecordClass<TValue>(ReadOnlySpan<Error> errors) => From(errors);
}
