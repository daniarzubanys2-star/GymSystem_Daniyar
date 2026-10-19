using GymSystem;
using Xunit;

namespace GymSystem.Tests;

public class ResultTests
{
    private static readonly AppError SomeError = AppError.NotFound("x");

    // MAP SUCCESS
    [Fact]
    public void Map_Success_TransformsValue()
    {
        var result = Result<int, AppError>.Ok(5).Map(v => v * 2);

        Assert.Equal(10, Assert.IsType<Result<int, AppError>.Success>(result).Value);
    }

    // BIND SUCCESS: the Result returned by the next step is preserved
    [Fact]
    public void Bind_Success_ReturnsResultOfNextStep()
    {
        var ok = Result<int, AppError>.Ok(5).Bind(v => Result<string, AppError>.Ok("value " + v));
        var fail = Result<int, AppError>.Ok(5).Bind(v => Result<string, AppError>.Fail(SomeError));

        Assert.Equal("value 5", Assert.IsType<Result<string, AppError>.Success>(ok).Value);
        Assert.Equal(SomeError, Assert.IsType<Result<string, AppError>.Failure>(fail).Error);
    }

    // FAILURE PROPAGATION: the supplied functions are NOT called (counter proves it)
    [Fact]
    public void MapAndBind_OnFailure_KeepTheFailure_AndDoNotCallTheFunctions()
    {
        int calls = 0;
        var failure = Result<int, AppError>.Fail(SomeError);

        var mapped = failure.Map(v => { calls++; return v + 1; });
        var bound = failure.Bind(v => { calls++; return Result<int, AppError>.Ok(v); });

        Assert.Equal(0, calls);
        Assert.Equal(SomeError, Assert.IsType<Result<int, AppError>.Failure>(mapped).Error);
        Assert.Equal(SomeError, Assert.IsType<Result<int, AppError>.Failure>(bound).Error);
    }

    [Fact]
    public void Chain_StopsAtFirstFailure_LaterStepsAreSkipped()
    {
        int laterCalls = 0;

        var result = Result<int, AppError>.Ok(1)
            .Bind(v => Result<int, AppError>.Fail(SomeError))
            .Bind(v => { laterCalls++; return Result<int, AppError>.Ok(v); })
            .Map(v => { laterCalls++; return v; });

        Assert.IsType<Result<int, AppError>.Failure>(result);
        Assert.Equal(0, laterCalls);
    }

    [Fact]
    public void Result_CannotBeCreatedWithMissingPayload()
    {
        Assert.Throws<ArgumentNullException>(() => Result<string, AppError>.Ok(null!));
        Assert.Throws<ArgumentNullException>(() => Result<string, AppError>.Fail(null!));
    }

    [Fact]
    public void ClientValidation_ConvertsToResult()
    {
        var ok = Client.Create("Anna", 30, MembershipTier.Basic, 3, 0).ToResult();
        var bad = ClientParser.Parse("Gina,abc,Basic,3,0").ToResult();

        Assert.IsType<Result<Client, AppError>.Success>(ok);
        Assert.Equal("invalid_client", Assert.IsType<Result<Client, AppError>.Failure>(bad).Error.Code);
    }
}
