using System.Collections.Immutable;
using System.Runtime.InteropServices;
using ResultWithUnion.Common;

namespace ResultWithUnion.VoidBasic;

public record struct ResultRecordStruct
{
    public bool IsSuccess { get; private set; }

    private readonly ImmutableArray<Error> _errors;
    public ImmutableArray<Error> Errors => IsSuccess
        ? throw new InvalidOperationException("Errors can be retrieved only when the result is an error")
        : _errors.IsDefault ? ImmutableArray<Error>.Empty : _errors;

    public ResultRecordStruct()
    {
        _errors = default;
        IsSuccess = true;
    }

    public ResultRecordStruct(ImmutableArray<Error> errors)
    {
        _errors = errors;
        IsSuccess = false;
    }

    public static ResultRecordStruct From(Error error) => new(ImmutableArray.Create(error));

    public static ResultRecordStruct From(Error[] errors) => new(ImmutableArray.Create(errors.AsSpan()));

    public static ResultRecordStruct From(List<Error> errors) => new(ImmutableArray.Create(CollectionsMarshal.AsSpan(errors)));

    public static ResultRecordStruct From(IEnumerable<Error> errors) => new(ErrorCollection.Create(errors));

    public static ResultRecordStruct From(params ReadOnlySpan<Error> errors) => new(ImmutableArray.Create(errors));
}
