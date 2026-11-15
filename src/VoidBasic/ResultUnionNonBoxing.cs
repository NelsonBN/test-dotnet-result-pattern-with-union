using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ResultWithUnion.Common;

namespace ResultWithUnion.VoidBasic;

// Hand-written union: real fields plus HasValue/TryGetValue, so no case is boxed. default is a success.
[Union]
public readonly struct ResultUnionNonBoxing : IUnion
{
    // default(ImmutableArray) means "no errors set", so default(ResultUnionNonBoxing) is a success.
    private readonly ImmutableArray<Error> _errors;

    public ResultUnionNonBoxing(ImmutableArray<Error> errors)
        => _errors = errors.IsDefault ? ImmutableArray<Error>.Empty : errors;

    public object? Value => IsSuccess ? null : _errors;

    public bool IsSuccess => _errors.IsDefault;

    public ImmutableArray<Error> Errors => IsSuccess
        ? throw new InvalidOperationException("Errors can be retrieved only when the result is an error")
        : _errors;

    public static ResultUnionNonBoxing From(Error error) => new(ImmutableArray.Create(error));

    public static ResultUnionNonBoxing From(Error[] errors) => new(ImmutableArray.Create(errors.AsSpan()));

    public static ResultUnionNonBoxing From(List<Error> errors) => new(ImmutableArray.Create(CollectionsMarshal.AsSpan(errors)));

    public static ResultUnionNonBoxing From(IEnumerable<Error> errors) => new(ErrorCollection.Create(errors));

    public static ResultUnionNonBoxing From(params ReadOnlySpan<Error> errors) => new(ImmutableArray.Create(errors));
}
