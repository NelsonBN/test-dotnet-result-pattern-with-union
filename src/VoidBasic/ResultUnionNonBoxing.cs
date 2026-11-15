using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ResultWithUnion.Common;

namespace ResultWithUnion.VoidBasic;

// Hand-written union: real fields plus HasValue/TryGetValue, so no case is boxed. default is a success.
[Union]
public readonly struct ResultUnionNonBoxing : IUnion
{
    private const byte NoneTag = 0;
    private const byte ErrorsTag = 1;

    private readonly ImmutableArray<Error> _errors;
    private readonly byte _tag;

    public ResultUnionNonBoxing(Error error)
    {
        _errors = ImmutableArray.Create(error);
        _tag = ErrorsTag;
    }

    public ResultUnionNonBoxing(ImmutableArray<Error> errors)
    {
        _errors = errors;
        _tag = ErrorsTag;
    }

    public object? Value => _tag switch
    {
        ErrorsTag => _errors,
        _ => null,
    };

    public bool IsSuccess => _tag == NoneTag;

    public ImmutableArray<Error> Errors => _tag == NoneTag
        ? throw new InvalidOperationException("Errors can be retrieved only when the result is an error")
        : _errors.IsDefault ? ImmutableArray<Error>.Empty : _errors;

    public static ResultUnionNonBoxing From(Error[] errors) => new(ImmutableArray.Create(errors.AsSpan()));

    public static ResultUnionNonBoxing From(List<Error> errors) => new(ImmutableArray.Create(CollectionsMarshal.AsSpan(errors)));

    public static ResultUnionNonBoxing From(IEnumerable<Error> errors) => new(ErrorCollection.Create(errors));

    public static ResultUnionNonBoxing From(params ReadOnlySpan<Error> errors) => new(ImmutableArray.Create(errors));
}
