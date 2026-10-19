using GymSystem;
using Xunit;

namespace GymSystem.Tests;

public class EnrollmentAndLookupTests
{
    // COMPLETE SUCCESS
    [Fact]
    public void Enrollment_CompleteChain_ProducesConfirmedText()
    {
        var result = Enrollment.EnrollByName(TestData.Sample(), "anna", PricingPolicies.Standard, 190);

        Assert.Equal("Confirmed: Anna paid 190",
                     Assert.IsType<Result<string, AppError>.Success>(result).Value);
    }

    // DOMAIN FAILURES (each with a useful error)
    [Theory]
    [InlineData("Zed", 100, "client_not_found")]
    [InlineData("", 100, "client_not_found")]
    [InlineData(null, 100, "client_not_found")]
    [InlineData("Boris", 100, "not_eligible")]
    [InlineData("Chen", 29, "payment_mismatch")]
    public void Enrollment_DomainFailures_ReportTheRule(string? name, int payment, string expectedCode)
    {
        var result = Enrollment.EnrollByName(TestData.Sample(), name, PricingPolicies.Standard, payment);

        var failure = Assert.IsType<Result<string, AppError>.Failure>(result);
        Assert.Equal(expectedCode, failure.Error.Code);
        Assert.Equal(ErrorKind.Domain, failure.Error.Kind);
        Assert.False(string.IsNullOrWhiteSpace(failure.Error.Message));
    }

    // LOOKUP PRESENT AND ABSENT
    [Fact]
    public void Lookup_FindsPresentClient_AndReportsAbsentOne()
    {
        var present = ClientLookup.FindByName(TestData.Sample(), " Dana ");
        var absent = ClientLookup.FindByName(TestData.Sample(), "Zed");

        Assert.Equal("Dana", Assert.IsType<Maybe<Client>.Some>(present).Value.Name);
        Assert.IsType<Maybe<Client>.None>(absent);
    }

    [Fact]
    public void Lookup_ZeroIsALegitimateValue_NotAbsence()
    {
        var chen = ClientLookup.FindTrainerSessions(TestData.Sample(), "Chen");   // 0 sessions
        var zed = ClientLookup.FindTrainerSessions(TestData.Sample(), "Zed");

        Assert.Equal(0, Assert.IsType<Maybe<int>.Some>(chen).Value);
        Assert.IsType<Maybe<int>.None>(zed);
    }

    [Fact]
    public void MissingLookup_IsConvertedToResultFailure_AndNextStepsAreSkipped()
    {
        int calls = 0;
        var result = ClientLookup.FindByName(new List<Client>(), "Anna")
            .ToResult(AppError.NotFound("Anna"))
            .Bind(c => { calls++; return MembershipWorkflow.Start(c, PricingPolicies.Standard); });

        Assert.IsType<Result<MembershipState, AppError>.Failure>(result);
        Assert.Equal(0, calls);
    }
}
