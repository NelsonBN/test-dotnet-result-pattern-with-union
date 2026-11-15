using System.Collections.Immutable;
using System.Runtime.InteropServices;
using ResultWithUnion.Common;

namespace ResultWithUnion.VoidBasic;

public sealed record class ResultRecordClass
{
    public bool IsSuccess { get; private set; }

    private readonly ImmutableArray<Error> _errors;
    public ImmutableArray<Error> Errors => IsSuccess
        ? throw new InvalidOperationException("Errors can be retrieved only when the result is an error")
        : _errors.IsDefault ? ImmutableArray<Error>.Empty : _errors;

    public ResultRecordClass()
    {
        _errors = default;
        IsSuccess = true;
    }

    public ResultRecordClass(ImmutableArray<Error> errors)
    {
        _errors = errors;
        IsSuccess = false;
    }

    public static ResultRecordClass From(Error error) => new(ImmutableArray.Create(error));

    public ResultRecordClass(Error[] errors)
        : this(ImmutableArray.Create(errors.AsSpan()))
    {
    }

    public ResultRecordClass(List<Error> errors)
        : this(ImmutableArray.Create(CollectionsMarshal.AsSpan(errors)))
    {
    }

    public ResultRecordClass(IEnumerable<Error> errors)
        : this(ErrorCollection.Create(errors))
    {
    }

    public ResultRecordClass(params ReadOnlySpan<Error> errors)
        : this(ImmutableArray.Create(errors))
    {
    }
}
