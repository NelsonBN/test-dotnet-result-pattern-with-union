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

    public ResultStruct(Error error)
        : this(ImmutableArray.Create(error))
    {
    }

    public ResultStruct(Error[] errors)
        : this(ImmutableArray.Create(errors.AsSpan()))
    {
    }

    public ResultStruct(List<Error> errors)
        : this(ImmutableArray.Create(CollectionsMarshal.AsSpan(errors)))
    {
    }

    public ResultStruct(IEnumerable<Error> errors)
        : this(ErrorCollection.Create(errors))
    {
    }

    public ResultStruct(params ReadOnlySpan<Error> errors)
        : this(ImmutableArray.Create(errors))
    {
    }
}
