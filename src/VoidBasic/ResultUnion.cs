using System.Collections.Immutable;
using System.Runtime.InteropServices;
using ResultWithUnion.Common;

namespace ResultWithUnion.VoidBasic;

public union ResultUnion(Error, ImmutableArray<Error>)
{
    public bool IsSuccess => this is null;

    public ImmutableArray<Error> Errors => this switch
    {
        Error error => ImmutableArray.Create(error),
        ImmutableArray<Error> errors => errors.IsDefault ? ImmutableArray<Error>.Empty : errors,
        null => throw new InvalidOperationException("Errors can be retrieved only when the result is an error"),
    };

    public static ResultUnion From(Error[] errors) => new(ImmutableArray.Create(errors.AsSpan()));

    public static ResultUnion From(List<Error> errors) => new(ImmutableArray.Create(CollectionsMarshal.AsSpan(errors)));

    public static ResultUnion From(IEnumerable<Error> errors) => new(ErrorCollection.Create(errors));

    public static ResultUnion From(params ReadOnlySpan<Error> errors) => new(ImmutableArray.Create(errors));
}
