using WIMS.Shared.Results;

namespace WIMS.Application.Tests.Shared;

public class ResultTests
{
    [Fact]
    public void Success_HasNoError()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(Error.None, result.Error);
    }

    [Fact]
    public void Failure_CarriesError()
    {
        var error = Error.NotFound("Item.NotFound", "الصنف غير موجود.");

        var result = Result.Failure(error);

        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void SuccessOfT_ExposesValue()
    {
        Result<int> result = 42;

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void FailureOfT_AccessingValue_Throws()
    {
        Result<int> result = Error.Validation("X", "خطأ");

        Assert.True(result.IsFailure);
        Assert.Throws<InvalidOperationException>(() => _ = result.Value);
    }

    [Fact]
    public void ImplicitConversion_FromError_ProducesFailure()
    {
        Result<string> result = Error.Conflict("Dup", "مكرر");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }

    [Fact]
    public void Success_WithError_Throws()
        => Assert.Throws<InvalidOperationException>(() => Result.Failure(Error.None));
}
