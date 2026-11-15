using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace ResultUnion;

// T must not overlap ImmutableArray<Error> (e.g. object) and a null success is read as a failure.
public union Result<T>(T, ImmutableArray<Error>)
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

    public static Result<T> From(Error error) => new(ImmutableArray.Create(error));

    public static Result<T> From(Error[] errors) => new(ImmutableArray.Create(errors.AsSpan()));

    public static Result<T> From(List<Error> errors) => new(ImmutableArray.Create(CollectionsMarshal.AsSpan(errors)));

    public static Result<T> From(IEnumerable<Error> errors) => new(ErrorCollection.Create(errors));

    public static Result<T> From(params ReadOnlySpan<Error> errors) => new(ImmutableArray.Create(errors));

    public static implicit operator Result<T>(Error error) => From(error);

    public static implicit operator Result<T>(Error[] errors) => From(errors);

    public static implicit operator Result<T>(List<Error> errors) => From(errors);

    public static implicit operator Result<T>(ReadOnlySpan<Error> errors) => From(errors);
}

public union Result(ImmutableArray<Error>)
{
    public bool IsSuccess => this is null;

    public ImmutableArray<Error> Errors => this switch
    {
        ImmutableArray<Error> errors => errors.IsDefault ? ImmutableArray<Error>.Empty : errors,
        null => throw new InvalidOperationException("Errors can be retrieved only when the result is an error"),
    };

    public static Result From(Error error) => new(ImmutableArray.Create(error));

    public static Result From(Error[] errors) => new(ImmutableArray.Create(errors.AsSpan()));

    public static Result From(List<Error> errors) => new(ImmutableArray.Create(CollectionsMarshal.AsSpan(errors)));

    public static Result From(IEnumerable<Error> errors) => new(ErrorCollection.Create(errors));

    public static Result From(params ReadOnlySpan<Error> errors) => new(ImmutableArray.Create(errors));

    public static implicit operator Result(Error error) => From(error);

    public static implicit operator Result(Error[] errors) => From(errors);

    public static implicit operator Result(List<Error> errors) => From(errors);

    public static implicit operator Result(ReadOnlySpan<Error> errors) => From(errors);
}
