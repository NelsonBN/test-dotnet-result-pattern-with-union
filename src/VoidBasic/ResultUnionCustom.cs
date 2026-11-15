using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ResultWithUnion.Common;

namespace ResultWithUnion.VoidBasic;

// Hand-written union: real fields plus HasValue/TryGetValue, so no case is boxed. default is a success.
[Union]
public readonly struct ResultUnionCustom : IUnion
{
    // default(ImmutableArray) means "no errors set", so default(ResultUnionCustom) is a success.
    private readonly ImmutableArray<Error> _errors;

    public ResultUnionCustom(ImmutableArray<Error> errors)
        => _errors = errors.IsDefault ? ImmutableArray<Error>.Empty : errors;

    public object? Value => IsSuccess ? null : _errors;

    public bool IsSuccess => _errors.IsDefault;

    public ImmutableArray<Error> Errors => IsSuccess
        ? throw new InvalidOperationException("Errors can be retrieved only when the result is an error")
        : _errors;

    public static ResultUnionCustom From(Error error) => new(ImmutableArray.Create(error));

    public static ResultUnionCustom From(Error[] errors) => new(ImmutableArray.Create(errors.AsSpan()));

    public static ResultUnionCustom From(List<Error> errors) => new(ImmutableArray.Create(CollectionsMarshal.AsSpan(errors)));

    public static ResultUnionCustom From(IEnumerable<Error> errors) => new(ErrorCollection.Create(errors));

    public static ResultUnionCustom From(params ReadOnlySpan<Error> errors) => new(ImmutableArray.Create(errors));

    public static implicit operator ResultUnionCustom(Error error) => From(error);

    public static implicit operator ResultUnionCustom(Error[] errors) => From(errors);

    public static implicit operator ResultUnionCustom(List<Error> errors) => From(errors);

    public static implicit operator ResultUnionCustom(ReadOnlySpan<Error> errors) => From(errors);
}
