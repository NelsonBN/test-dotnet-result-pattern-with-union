using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace ResultClass.Tests;

public sealed class ResultTests
{
    private static readonly Error _first = new("first");
    private static readonly Error _second = new("second");

    private const string VALUE_MESSAGE = "*Value can be retrieved only when the result is not an error*";
    private const string ERRORS_MESSAGE = "*Errors can be retrieved only when the result is an error*";

    [Fact]
    public void Given_Value_When_Constructed_Then_IsSuccessAndValueIsReturned()
    {
        // Arrange
        var result = new Result<string>("ok");

        // Act
        var value = result.Value;

        // Assert
        result.IsSuccess.Should().BeTrue();
        value.Should().Be("ok");
    }

    [Fact]
    public void Given_NullValue_When_Constructed_Then_IsSuccessAndValueIsNull()
    {
        // Arrange
        var result = new Result<string?>((string?)null);

        // Act
        var value = result.Value;

        // Assert
        result.IsSuccess.Should().BeTrue();
        value.Should().BeNull();
    }

    [Fact]
    public void Given_SuccessfulResult_When_ErrorsRead_Then_Throws()
    {
        // Arrange
        var result = new Result<string>("ok");

        // Act & Assert
        var act = () => result.Errors;
        act.Should().Throw<InvalidOperationException>().WithMessage(ERRORS_MESSAGE);
    }

    [Fact]
    public void Given_ErrorsArray_When_Constructed_Then_IsFailureAndErrorsAreTheSameArray()
    {
        // Arrange
        ImmutableArray<Error> errors = [_first, _second];
        var result = new Result<string>(errors);

        // Act
        var actual = result.Errors;

        // Assert
        result.IsSuccess.Should().BeFalse();
        ImmutableCollectionsMarshal.AsArray(actual).Should().BeSameAs(ImmutableCollectionsMarshal.AsArray(errors));
    }

    [Fact]
    public void Given_DefaultErrorsArray_When_Constructed_Then_ErrorsAreEmpty()
    {
        // Arrange
        var result = new Result<string>(default(ImmutableArray<Error>));

        // Act
        var actual = result.Errors;

        // Assert
        result.IsSuccess.Should().BeFalse();
        actual.IsDefault.Should().BeFalse();
        actual.Should().BeEmpty();
    }

    [Fact]
    public void Given_FailedResult_When_ValueRead_Then_Throws()
    {
        // Arrange
        Result<string> result = _first;

        // Act & Assert
        var act = () => result.Value;
        act.Should().Throw<InvalidOperationException>().WithMessage(VALUE_MESSAGE);
    }

    [Fact]
    public void Given_Error_When_From_Then_ResultHoldsThatError()
    {
        // Act
        var result = Result<string>.From(_first);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Equal(_first);
    }

    [Fact]
    public void Given_ErrorArray_When_From_Then_ResultHoldsCopyOfErrors()
    {
        // Arrange
        Error[] errors = [_first, _second];

        // Act
        var result = Result<string>.From(errors);
        errors[0] = _second;

        // Assert
        result.Errors.Should().Equal(_first, _second);
    }

    [Fact]
    public void Given_ErrorList_When_From_Then_ResultHoldsCopyOfErrors()
    {
        // Arrange
        List<Error> errors = [_first, _second];

        // Act
        var result = Result<string>.From(errors);
        errors[0] = _second;

        // Assert
        result.Errors.Should().Equal(_first, _second);
    }

    [Fact]
    public void Given_ErrorEnumerable_When_From_Then_ResultHoldsErrors()
    {
        // Arrange
        IEnumerable<Error> errors = new Queue<Error>([_first, _second]);

        // Act
        var result = Result<string>.From(errors);

        // Assert
        result.Errors.Should().Equal(_first, _second);
    }

    [Fact]
    public void Given_ErrorSpan_When_From_Then_ResultHoldsCopyOfErrors()
    {
        // Arrange
        Error[] errors = [_first, _second];

        // Act
        var result = Result<string>.From(errors.AsSpan());
        errors[0] = _second;

        // Assert
        result.Errors.Should().Equal(_first, _second);
    }

    [Fact]
    public void Given_ParamsErrors_When_From_Then_ResultHoldsErrors()
    {
        // Act
        var result = Result<string>.From(_first, _second);

        // Assert
        result.Errors.Should().Equal(_first, _second);
    }

    [Fact]
    public void Given_Error_When_ImplicitlyConverted_Then_ResultHoldsThatError()
    {
        // Act
        Result<string> result = _first;

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Equal(_first);
    }

    [Fact]
    public void Given_ErrorArray_When_ImplicitlyConverted_Then_ResultHoldsErrors()
    {
        // Arrange
        Error[] errors = [_first, _second];

        // Act
        Result<string> result = errors;

        // Assert
        result.Errors.Should().Equal(_first, _second);
    }

    [Fact]
    public void Given_ErrorList_When_ImplicitlyConverted_Then_ResultHoldsErrors()
    {
        // Arrange
        List<Error> errors = [_first, _second];

        // Act
        Result<string> result = errors;

        // Assert
        result.Errors.Should().Equal(_first, _second);
    }

    [Fact]
    public void Given_ErrorSpan_When_ImplicitlyConverted_Then_ResultHoldsErrors()
    {
        // Arrange
        Error[] errors = [_first, _second];

        // Act
        Result<string> result = errors.AsSpan();

        // Assert
        result.Errors.Should().Equal(_first, _second);
    }

    [Fact]
    public void Given_SuccessfulResult_When_TryGetValue_Then_ReturnsValue()
    {
        // Arrange
        var result = new Result<string>("ok");

        // Act
        var found = result.TryGetValue(out string value);

        // Assert
        found.Should().BeTrue();
        value.Should().Be("ok");
    }

    [Fact]
    public void Given_SuccessfulResult_When_TryGetValueErrors_Then_ReturnsFalseWithEmptyErrors()
    {
        // Arrange
        var result = new Result<string>("ok");

        // Act
        var found = result.TryGetValue(out ImmutableArray<Error> errors);

        // Assert
        found.Should().BeFalse();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Given_FailedResult_When_TryGetValue_Then_ReturnsFalse()
    {
        // Arrange
        Result<string> result = _first;

        // Act
        var found = result.TryGetValue(out string _);

        // Assert
        found.Should().BeFalse();
    }

    [Fact]
    public void Given_FailedResult_When_TryGetValueErrors_Then_ReturnsErrors()
    {
        // Arrange
        Result<string> result = _first;

        // Act
        var found = result.TryGetValue(out ImmutableArray<Error> errors);

        // Assert
        found.Should().BeTrue();
        errors.Should().Equal(_first);
    }

    [Fact]
    public void Given_AnyResult_When_HasValueRead_Then_IsTrue()
    {
        // Arrange
        var result = new Result<string>("ok");

        // Act
        var hasValue = result.HasValue;

        // Assert
        hasValue.Should().BeTrue();
    }


    [Fact]
    public void Given_NoErrors_When_VoidConstructed_Then_IsSuccess()
    {
        // Act
        var result = new Result();

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Given_SuccessfulVoidResult_When_ErrorsRead_Then_Throws()
    {
        // Arrange
        var result = new Result();

        // Act & Assert
        var act = () => result.Errors;
        act.Should().Throw<InvalidOperationException>().WithMessage(ERRORS_MESSAGE);
    }

    [Fact]
    public void Given_ErrorsArray_When_VoidConstructed_Then_IsFailureAndErrorsAreTheSameArray()
    {
        // Arrange
        ImmutableArray<Error> errors = [_first, _second];
        var result = new Result(errors);

        // Act
        var actual = result.Errors;

        // Assert
        result.IsSuccess.Should().BeFalse();
        ImmutableCollectionsMarshal.AsArray(actual).Should().BeSameAs(ImmutableCollectionsMarshal.AsArray(errors));
    }

    [Fact]
    public void Given_DefaultErrorsArray_When_VoidConstructed_Then_ErrorsAreEmpty()
    {
        // Arrange
        ImmutableArray<Error> errors = default;
        var result = new Result(errors);

        // Act
        var actual = result.Errors;

        // Assert
        result.IsSuccess.Should().BeFalse();
        actual.Should().BeEmpty();
    }

    [Fact]
    public void Given_Error_When_VoidFrom_Then_ResultHoldsThatError()
    {
        // Act
        var result = Result.From(_first);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Equal(_first);
    }

    [Fact]
    public void Given_ErrorArray_When_VoidFrom_Then_ResultHoldsCopyOfErrors()
    {
        // Arrange
        Error[] errors = [_first, _second];

        // Act
        var result = Result.From(errors);
        errors[0] = _second;

        // Assert
        result.Errors.Should().Equal(_first, _second);
    }

    [Fact]
    public void Given_ErrorList_When_VoidFrom_Then_ResultHoldsCopyOfErrors()
    {
        // Arrange
        List<Error> errors = [_first, _second];

        // Act
        var result = Result.From(errors);
        errors[0] = _second;

        // Assert
        result.Errors.Should().Equal(_first, _second);
    }

    [Fact]
    public void Given_ErrorEnumerable_When_VoidFrom_Then_ResultHoldsErrors()
    {
        // Arrange
        IEnumerable<Error> errors = new Queue<Error>([_first, _second]);

        // Act
        var result = Result.From(errors);

        // Assert
        result.Errors.Should().Equal(_first, _second);
    }

    [Fact]
    public void Given_ErrorSpan_When_VoidFrom_Then_ResultHoldsCopyOfErrors()
    {
        // Arrange
        Error[] errors = [_first, _second];

        // Act
        var result = Result.From(errors.AsSpan());
        errors[0] = _second;

        // Assert
        result.Errors.Should().Equal(_first, _second);
    }

    [Fact]
    public void Given_ParamsErrors_When_VoidFrom_Then_ResultHoldsErrors()
    {
        // Act
        var result = Result.From(_first, _second);

        // Assert
        result.Errors.Should().Equal(_first, _second);
    }

    [Fact]
    public void Given_Error_When_VoidImplicitlyConverted_Then_ResultHoldsThatError()
    {
        // Act
        Result result = _first;

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Equal(_first);
    }

    [Fact]
    public void Given_ErrorArray_When_VoidImplicitlyConverted_Then_ResultHoldsErrors()
    {
        // Arrange
        Error[] errors = [_first, _second];

        // Act
        Result result = errors;

        // Assert
        result.Errors.Should().Equal(_first, _second);
    }

    [Fact]
    public void Given_ErrorList_When_VoidImplicitlyConverted_Then_ResultHoldsErrors()
    {
        // Arrange
        List<Error> errors = [_first, _second];

        // Act
        Result result = errors;

        // Assert
        result.Errors.Should().Equal(_first, _second);
    }

    [Fact]
    public void Given_ErrorSpan_When_VoidImplicitlyConverted_Then_ResultHoldsErrors()
    {
        // Arrange
        Error[] errors = [_first, _second];

        // Act
        Result result = errors.AsSpan();

        // Assert
        result.Errors.Should().Equal(_first, _second);
    }

    [Fact]
    public void Given_SuccessfulVoidResult_When_TryGetValueErrors_Then_ReturnsFalseWithEmptyErrors()
    {
        // Arrange
        var result = new Result();

        // Act
        var found = result.TryGetValue(out var errors);

        // Assert
        found.Should().BeFalse();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Given_FailedVoidResult_When_TryGetValueErrors_Then_ReturnsErrors()
    {
        // Arrange
        Result result = _first;

        // Act
        var found = result.TryGetValue(out var errors);

        // Assert
        found.Should().BeTrue();
        errors.Should().Equal(_first);
    }

    [Fact]
    public void Given_SuccessfulVoidResult_When_HasValueRead_Then_IsFalse()
    {
        // Arrange
        var result = new Result();

        // Act
        var hasValue = result.HasValue;

        // Assert
        hasValue.Should().BeFalse();
    }

    [Fact]
    public void Given_FailedVoidResult_When_HasValueRead_Then_IsTrue()
    {
        // Arrange
        Result result = _first;

        // Act
        var hasValue = result.HasValue;

        // Assert
        hasValue.Should().BeTrue();
    }
}
