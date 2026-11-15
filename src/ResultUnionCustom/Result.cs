using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ResultUnionCustom;

// Hand-written union: real fields plus HasValue/TryGetValue, so no case is boxed.
// T must not overlap ImmutableArray<Error> (e.g. object) and a null success is read as a failure.
[Union]
public readonly struct Result<T> : IUnion
{
    private readonly T? _value;
    private readonly ImmutableArray<Error> _errors;

    public Result(T value)
    {
        if(value is null)
        {
            return;
        }

        _value = value;
        IsSuccess = true;
    }

    public Result(ImmutableArray<Error> errors)
        => _errors = errors.IsDefault ? ImmutableArray<Error>.Empty : errors;

    public readonly bool IsSuccess { get; }

    public object? Value => IsSuccess
        ? _value
        : _errors.IsDefault ? null : _errors;

    public bool HasValue => IsSuccess || !_errors.IsDefault;

    public bool TryGetValue(out T value)
    {
        value = _value!;
        return IsSuccess;
    }

    public bool TryGetValue(out ImmutableArray<Error> value)
    {
        value = _errors;
        return !IsSuccess && !_errors.IsDefault;
    }

    public T GetValue() => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Value can be retrieved only when the result is not an error");

    public ImmutableArray<Error> Errors => IsSuccess
        ? throw new InvalidOperationException("Errors can be retrieved only when the result is an error")
        : _errors.IsDefault ? ImmutableArray<Error>.Empty : _errors;

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

// Hand-written union: real fields plus HasValue/TryGetValue, so no case is boxed. default is a success.
[Union]
public readonly struct Result : IUnion
{
    // default(ImmutableArray) means "no errors set", so default(Result) is a success.
    private readonly ImmutableArray<Error> _errors;

    public Result(ImmutableArray<Error> errors)
        => _errors = errors.IsDefault ? ImmutableArray<Error>.Empty : errors;

    public object? Value => IsSuccess ? null : _errors;

    public bool HasValue => !IsSuccess;

    public bool TryGetValue(out ImmutableArray<Error> value)
    {
        value = _errors;
        return !IsSuccess;
    }

    public bool IsSuccess => _errors.IsDefault;

    public ImmutableArray<Error> Errors => IsSuccess
        ? throw new InvalidOperationException("Errors can be retrieved only when the result is an error")
        : _errors;

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
