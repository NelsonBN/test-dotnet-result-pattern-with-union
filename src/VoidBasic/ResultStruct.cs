using System.Collections.Immutable;
using System.Runtime.InteropServices;
using ResultWithUnion.Common;

namespace ResultWithUnion.VoidBasic;

public struct ResultStruct
{
    public bool IsSuccess { get; private set; }

    private readonly ImmutableArray<Error> _errors;
    public ImmutableArray<Error> Errors => IsSuccess
        ? throw new InvalidOperationException("Errors can be retrieved only when the result is an error")
        : _errors.IsDefault ? ImmutableArray<Error>.Empty : _errors;

    public ResultStruct()
    {
        _errors = default;
        IsSuccess = true;
    }

    public ResultStruct(ImmutableArray<Error> errors)
    {
        _errors = errors;
        IsSuccess = false;
    }

    public static ResultStruct From(Error error) => new(ImmutableArray.Create(error));

    public static ResultStruct From(Error[] errors) => new(ImmutableArray.Create(errors.AsSpan()));

    public static ResultStruct From(List<Error> errors) => new(ImmutableArray.Create(CollectionsMarshal.AsSpan(errors)));

    public static ResultStruct From(IEnumerable<Error> errors) => new(ErrorCollection.Create(errors));

    public static ResultStruct From(params ReadOnlySpan<Error> errors) => new(ImmutableArray.Create(errors));

    public static implicit operator ResultStruct(Error error) => From(error);

    public static implicit operator ResultStruct(Error[] errors) => From(errors);

    public static implicit operator ResultStruct(List<Error> errors) => From(errors);

    public static implicit operator ResultStruct(ReadOnlySpan<Error> errors) => From(errors);
}
