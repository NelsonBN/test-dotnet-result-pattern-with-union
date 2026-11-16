using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ResultUnionCustomExtreme;

// Hand-written union: real fields plus HasValue/TryGetValue, so no case is boxed.
// T must not overlap ImmutableArray<Error> (e.g. object) and a null success is read as a failure.
[Union]
public readonly struct Result<T> : IUnion
{
    private readonly T? _value;
    private readonly ImmutableArray<Error> _errors;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Result(T value)
    {
        if(value is null)
        {
            return;
        }

        _value = value;
        IsSuccess = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Result(ImmutableArray<Error> errors)
        => _errors = errors.IsDefault ? [] : errors;

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
        : _errors.IsDefault ? [] : _errors;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result<T> From(Error error) => new([error]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result<T> From(Error[] errors) => new([.. errors]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result<T> From(List<Error> errors) => new([.. CollectionsMarshal.AsSpan(errors)]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result<T> From(IEnumerable<Error> errors) => new(ErrorCollection.Create(errors));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result<T> From(params ReadOnlySpan<Error> errors) => new([.. errors]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Result<T>(Error error) => From(error);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Result<T>(Error[] errors) => From(errors);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Result<T>(List<Error> errors) => From(errors);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Result<T>(ReadOnlySpan<Error> errors) => From(errors);
}

// Hand-written union: real fields plus HasValue/TryGetValue, so no case is boxed. default is a success.
[Union]
[method: MethodImpl(MethodImplOptions.AggressiveInlining)]
public readonly struct Result(ImmutableArray<Error> errors) : IUnion
{
    // default(ImmutableArray) means "no errors set", so default(Result) is a success.
    private readonly ImmutableArray<Error> _errors = errors.IsDefault ? [] : errors;

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result From(Error error) => new([error]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result From(Error[] errors) => new([.. errors]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result From(List<Error> errors) => new([.. CollectionsMarshal.AsSpan(errors)]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result From(IEnumerable<Error> errors) => new(ErrorCollection.Create(errors));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result From(params ReadOnlySpan<Error> errors) => new([.. errors]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Result(Error error) => From(error);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Result(Error[] errors) => From(errors);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Result(List<Error> errors) => From(errors);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Result(ReadOnlySpan<Error> errors) => From(errors);
}
