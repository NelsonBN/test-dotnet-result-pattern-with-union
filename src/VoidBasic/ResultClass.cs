using System.Collections.Immutable;
using System.Runtime.InteropServices;
using ResultWithUnion.Common;

namespace ResultWithUnion.VoidBasic;

public sealed class ResultClass
{
    public bool IsSuccess { get; private set; }

    private readonly ImmutableArray<Error> _errors;
    public ImmutableArray<Error> Errors => IsSuccess
        ? throw new InvalidOperationException("Errors can be retrieved only when the result is an error")
        : _errors.IsDefault ? ImmutableArray<Error>.Empty : _errors;

    public ResultClass()
    {
        _errors = default;
        IsSuccess = true;
    }

    public ResultClass(ImmutableArray<Error> errors)
    {
        _errors = errors;
        IsSuccess = false;
    }

    public static ResultClass From(Error error) => new(ImmutableArray.Create(error));

    public static ResultClass From(Error[] errors) => new(ImmutableArray.Create(errors.AsSpan()));

    public static ResultClass From(List<Error> errors) => new(ImmutableArray.Create(CollectionsMarshal.AsSpan(errors)));

    public static ResultClass From(IEnumerable<Error> errors) => new(ErrorCollection.Create(errors));

    public static ResultClass From(params ReadOnlySpan<Error> errors) => new(ImmutableArray.Create(errors));
}
