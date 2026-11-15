using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace ResultRecordStruct;

public record struct Result<TValue>
{
    public bool IsSuccess { get; private set; }

    private readonly TValue? _value;
    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Value can be retrieved only when the result is not an error");

    private readonly ImmutableArray<Error> _errors;
    public ImmutableArray<Error> Errors => IsSuccess
        ? throw new InvalidOperationException("Errors can be retrieved only when the result is an error")
        : _errors.IsDefault ? ImmutableArray<Error>.Empty : _errors;

    public Result(TValue value)
    {
        _value = value;
        _errors = default;
        IsSuccess = true;
    }

    public Result(ImmutableArray<Error> errors)
    {
        _value = default;
        _errors = errors;
        IsSuccess = false;
    }

    public bool HasValue => true;

    public bool TryGetValue(out TValue value)
    {
        value = _value!;
        return IsSuccess;
    }

    public bool TryGetValue(out ImmutableArray<Error> errors)
    {
        errors = _errors.IsDefault ? ImmutableArray<Error>.Empty : _errors;
        return !IsSuccess;
    }

    public static Result<TValue> From(Error error) => new(ImmutableArray.Create(error));

    public static Result<TValue> From(Error[] errors) => new(ImmutableArray.Create(errors.AsSpan()));

    public static Result<TValue> From(List<Error> errors) => new(ImmutableArray.Create(CollectionsMarshal.AsSpan(errors)));

    public static Result<TValue> From(IEnumerable<Error> errors) => new(ErrorCollection.Create(errors));

    public static Result<TValue> From(params ReadOnlySpan<Error> errors) => new(ImmutableArray.Create(errors));

    public static implicit operator Result<TValue>(Error error) => From(error);

    public static implicit operator Result<TValue>(Error[] errors) => From(errors);

    public static implicit operator Result<TValue>(List<Error> errors) => From(errors);

    public static implicit operator Result<TValue>(ReadOnlySpan<Error> errors) => From(errors);
}

public record struct Result
{
    public bool IsSuccess { get; private set; }

    private readonly ImmutableArray<Error> _errors;
    public ImmutableArray<Error> Errors => IsSuccess
        ? throw new InvalidOperationException("Errors can be retrieved only when the result is an error")
        : _errors.IsDefault ? ImmutableArray<Error>.Empty : _errors;

    public Result()
    {
        _errors = default;
        IsSuccess = true;
    }

    public Result(ImmutableArray<Error> errors)
    {
        _errors = errors;
        IsSuccess = false;
    }

    public bool HasValue => !IsSuccess;

    public bool TryGetValue(out ImmutableArray<Error> errors)
    {
        errors = _errors.IsDefault ? ImmutableArray<Error>.Empty : _errors;
        return !IsSuccess;
    }

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
