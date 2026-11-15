using System.Collections.Immutable;
using System.Runtime.InteropServices;
using ResultWithUnion.Common;

namespace ResultWithUnion.ValueBasic;

public record struct ResultRecordStruct<TValue>
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

    public ResultRecordStruct(TValue value)
    {
        _value = value;
        _errors = default;
        IsSuccess = true;
    }

    public ResultRecordStruct(ImmutableArray<Error> errors)
    {
        _value = default;
        _errors = errors;
        IsSuccess = false;
    }

    public static ResultRecordStruct<TValue> From(Error error) => new(ImmutableArray.Create(error));

    public ResultRecordStruct(Error[] errors)
        : this(ImmutableArray.Create(errors.AsSpan()))
    {
    }

    public ResultRecordStruct(List<Error> errors)
        : this(ImmutableArray.Create(CollectionsMarshal.AsSpan(errors)))
    {
    }

    public ResultRecordStruct(IEnumerable<Error> errors)
        : this(ErrorCollection.Create(errors))
    {
    }

    public ResultRecordStruct(params ReadOnlySpan<Error> errors)
        : this(ImmutableArray.Create(errors))
    {
    }
}
