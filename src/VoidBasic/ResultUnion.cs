using System.Collections.Immutable;
using System.Runtime.InteropServices;
using ResultWithUnion.Common;

namespace ResultWithUnion.VoidBasic;

public union ResultUnion(ImmutableArray<Error>)
{
    public bool IsSuccess => this is null;

    public ImmutableArray<Error> Errors => this switch
    {
        ImmutableArray<Error> errors => errors.IsDefault ? ImmutableArray<Error>.Empty : errors,
        null => throw new InvalidOperationException("Errors can be retrieved only when the result is an error"),
    };

    public static ResultUnion From(Error error) => new(ImmutableArray.Create(error));

    public static ResultUnion From(Error[] errors) => new(ImmutableArray.Create(errors.AsSpan()));

    public static ResultUnion From(List<Error> errors) => new(ImmutableArray.Create(CollectionsMarshal.AsSpan(errors)));

    public static ResultUnion From(IEnumerable<Error> errors) => new(ErrorCollection.Create(errors));

    public static ResultUnion From(params ReadOnlySpan<Error> errors) => new(ImmutableArray.Create(errors));

    public static implicit operator ResultUnion(Error error) => From(error);

    public static implicit operator ResultUnion(Error[] errors) => From(errors);

    public static implicit operator ResultUnion(List<Error> errors) => From(errors);

    public static implicit operator ResultUnion(ReadOnlySpan<Error> errors) => From(errors);
}
