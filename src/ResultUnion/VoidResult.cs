using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace ResultUnion;

public union VoidResult(ImmutableArray<Error>)
{
    public bool IsSuccess => this is null;

    public ImmutableArray<Error> Errors => this switch
    {
        ImmutableArray<Error> errors => errors.IsDefault ? ImmutableArray<Error>.Empty : errors,
        null => throw new InvalidOperationException("Errors can be retrieved only when the result is an error"),
    };

    public static VoidResult From(Error error) => new(ImmutableArray.Create(error));

    public static VoidResult From(Error[] errors) => new(ImmutableArray.Create(errors.AsSpan()));

    public static VoidResult From(List<Error> errors) => new(ImmutableArray.Create(CollectionsMarshal.AsSpan(errors)));

    public static VoidResult From(IEnumerable<Error> errors) => new(ErrorCollection.Create(errors));

    public static VoidResult From(params ReadOnlySpan<Error> errors) => new(ImmutableArray.Create(errors));

    public static implicit operator VoidResult(Error error) => From(error);

    public static implicit operator VoidResult(Error[] errors) => From(errors);

    public static implicit operator VoidResult(List<Error> errors) => From(errors);

    public static implicit operator VoidResult(ReadOnlySpan<Error> errors) => From(errors);
}
