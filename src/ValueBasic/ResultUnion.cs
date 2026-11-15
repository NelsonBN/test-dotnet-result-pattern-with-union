using System.Collections.Immutable;
using System.Runtime.InteropServices;
using ResultWithUnion.Common;

namespace ResultWithUnion.ValueBasic;

// T must not overlap ImmutableArray<Error> (e.g. object) and a null success is read as a failure.
public union ResultUnion<T>(T, ImmutableArray<Error>)
{
    public bool IsSuccess => this is T;

    public T GetValue() => this switch
    {
        T value => value,
        ImmutableArray<Error> => throw new InvalidOperationException("Value can be retrieved only when the result is not an error"),
        null => throw new InvalidOperationException("Value can be retrieved only when the result is not an error"),
    };

    public ImmutableArray<Error> Errors => this switch
    {
        ImmutableArray<Error> errors => errors.IsDefault ? ImmutableArray<Error>.Empty : errors,
        T => throw new InvalidOperationException("Errors can be retrieved only when the result is an error"),
        null => ImmutableArray<Error>.Empty,
    };

    public static ResultUnion<T> From(Error[] errors) => new(ImmutableArray.Create(errors.AsSpan()));

    public static ResultUnion<T> From(List<Error> errors) => new(ImmutableArray.Create(CollectionsMarshal.AsSpan(errors)));

    public static ResultUnion<T> From(IEnumerable<Error> errors) => new(ErrorCollection.Create(errors));

    public static ResultUnion<T> From(params ReadOnlySpan<Error> errors) => new(ImmutableArray.Create(errors));
}
