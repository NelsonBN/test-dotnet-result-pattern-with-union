using System.Collections.Immutable;
using System.Runtime.InteropServices;
using ResultWithUnion.Common;

namespace ResultWithUnion.VoidBasic;

public readonly struct ResultReadonlyStruct
{
    public readonly bool IsSuccess;

    private readonly ImmutableArray<Error> _errors;
    public ImmutableArray<Error> Errors => IsSuccess
        ? throw new InvalidOperationException("Errors can be retrieved only when the result is an error")
        : _errors.IsDefault ? ImmutableArray<Error>.Empty : _errors;

    public ResultReadonlyStruct()
    {
        _errors = default;
        IsSuccess = true;
    }

    public ResultReadonlyStruct(ImmutableArray<Error> errors)
    {
        _errors = errors;
        IsSuccess = false;
    }

    public static ResultReadonlyStruct From(Error error) => new(ImmutableArray.Create(error));

    public static ResultReadonlyStruct From(Error[] errors) => new(ImmutableArray.Create(errors.AsSpan()));

    public static ResultReadonlyStruct From(List<Error> errors) => new(ImmutableArray.Create(CollectionsMarshal.AsSpan(errors)));

    public static ResultReadonlyStruct From(IEnumerable<Error> errors) => new(ErrorCollection.Create(errors));

    public static ResultReadonlyStruct From(params ReadOnlySpan<Error> errors) => new(ImmutableArray.Create(errors));
}
