using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace ResultUnion;

// T must not overlap ImmutableArray<Error> (e.g. object) and a null success is read as a failure.
public union ValueResult<T>(T, ImmutableArray<Error>)
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

    public static ValueResult<T> From(Error error) => new(ImmutableArray.Create(error));

    public static ValueResult<T> From(Error[] errors) => new(ImmutableArray.Create(errors.AsSpan()));

    public static ValueResult<T> From(List<Error> errors) => new(ImmutableArray.Create(CollectionsMarshal.AsSpan(errors)));

    public static ValueResult<T> From(IEnumerable<Error> errors) => new(ErrorCollection.Create(errors));

    public static ValueResult<T> From(params ReadOnlySpan<Error> errors) => new(ImmutableArray.Create(errors));

    public static implicit operator ValueResult<T>(Error error) => From(error);

    public static implicit operator ValueResult<T>(Error[] errors) => From(errors);

    public static implicit operator ValueResult<T>(List<Error> errors) => From(errors);

    public static implicit operator ValueResult<T>(ReadOnlySpan<Error> errors) => From(errors);
}
