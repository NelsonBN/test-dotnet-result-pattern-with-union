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

    // Set by the errors constructor, so it stays a plain store; default(Result<T>) has neither case.
    private readonly bool _isError;

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
    {
        _errors = errors;
        _isError = true;
    }

    public readonly bool IsSuccess { get; }

    public object? Value => IsSuccess
        ? _value
        : _isError ? Errors : null;

    public bool HasValue => IsSuccess || _isError;

    public bool TryGetValue(out T value)
    {
        value = _value!;
        return IsSuccess;
    }

    public bool TryGetValue(out ImmutableArray<Error> value)
    {
        value = _isError && _errors.IsDefault ? [] : _errors;
        return _isError;
    }

    public T GetValue() => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Value can be retrieved only when the result is not an error");

    public ImmutableArray<Error> Errors => IsSuccess
        ? throw new InvalidOperationException("Errors can be retrieved only when the result is an error")
        : _errors.IsDefault ? [] : _errors;

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
    private readonly ImmutableArray<Error> _errors;

    // Set by the errors constructor, so default(Result) is a success and the constructor stays a plain store.
    private bool IsError { get; }

    public Result(ImmutableArray<Error> errors)
    {
        _errors = errors;
        IsError = true;
    }

    public object? Value => IsSuccess ? null : Errors;

    public bool HasValue => IsError;

    public bool TryGetValue(out ImmutableArray<Error> value)
    {
        value = IsError && _errors.IsDefault ? [] : _errors;
        return IsError;
    }

    public bool IsSuccess => !IsError;

    public ImmutableArray<Error> Errors => IsSuccess
        ? throw new InvalidOperationException("Errors can be retrieved only when the result is an error")
        : _errors.IsDefault ? [] : _errors;

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
