using GymSystem;
using Xunit;
using MResult = GymSystem.Result<GymSystem.MembershipState, GymSystem.AppError>;

namespace GymSystem.Tests;

public class MembershipWorkflowTests
{
    private static Client Anna() => TestData.Make("Anna", 30, MembershipTier.Standard, 3, 2);   // total 190

    private static MembershipState.Pending StartAnna()
    {
        var success = Assert.IsType<MResult.Success>(MembershipWorkflow.Start(Anna(), PricingPolicies.Standard));
        return Assert.IsType<MembershipState.Pending>(success.Value);
    }

    [Fact]
    public void NormalOperation_ValidClient_BecomesPending_ThenConfirmed()
    {
        MembershipState.Pending pending = StartAnna();
        Assert.Equal(190, pending.Quote.TotalCost);

        var success = Assert.IsType<MResult.Success>(MembershipWorkflow.Confirm(pending, 190));
        var confirmed = Assert.IsType<MembershipState.Confirmed>(success.Value);
        Assert.Equal(190, confirmed.PaidAmount);
    }

    [Fact]
    public void DistinctCases_DescribeGivesCaseSpecificText()
    {
        MembershipState.Pending pending = StartAnna();
        var confirmed = ((MResult.Success)MembershipWorkflow.Confirm(pending, 190)).Value;

        Assert.Equal("Pending: Anna must pay 190", MembershipWorkflow.Describe(pending));
        Assert.Equal("Confirmed: Anna paid 190", MembershipWorkflow.Describe(confirmed));
        Assert.Equal("No membership", MembershipWorkflow.Describe(null));
    }

    [Fact]
    public void DomainFailure_UnderMinimumAge_IsNotEligible()
    {
        var boris = TestData.Make("Boris", 15, MembershipTier.Basic, 6, 0);

        var failure = Assert.IsType<MResult.Failure>(MembershipWorkflow.Start(boris, PricingPolicies.Standard));

        Assert.Equal("not_eligible", failure.Error.Code);
        Assert.Equal(ErrorKind.Domain, failure.Error.Kind);
        Assert.Contains("Boris", failure.Error.Message);
    }

    [Fact]
    public void DomainFailure_ConfirmWithoutMembership_IsInvalidState()
    {
        var failure = Assert.IsType<MResult.Failure>(MembershipWorkflow.Confirm(null, 190));
        Assert.Equal("invalid_state", failure.Error.Code);
    }

    [Theory]
    [InlineData(189, false)]
    [InlineData(190, true)]
    [InlineData(191, false)]
    public void Boundary_PaymentMustEqualQuotedTotal(int payment, bool accepted)
    {
        MResult result = MembershipWorkflow.Confirm(StartAnna(), payment);

        if (accepted)
            Assert.IsType<MResult.Success>(result);
        else
            Assert.Equal("payment_mismatch", Assert.IsType<MResult.Failure>(result).Error.Code);
    }

    [Fact]
    public void RepeatedConfirmation_IsRejected_AndConfirmedStateStaysTheSame()
    {
        MembershipState confirmed = ((MResult.Success)MembershipWorkflow.Confirm(StartAnna(), 190)).Value;

        var second = Assert.IsType<MResult.Failure>(MembershipWorkflow.Confirm(confirmed, 190));

        Assert.Equal("invalid_state", second.Error.Code);
        Assert.Equal(190, Assert.IsType<MembershipState.Confirmed>(confirmed).PaidAmount);
    }

    [Fact]
    public void FailedTransition_LeavesOriginalStateUnchanged()
    {
        MembershipState.Pending pending = StartAnna();
        MembershipState.Pending copy = StartAnna();

        Assert.IsType<MResult.Failure>(MembershipWorkflow.Confirm(pending, 1));

        Assert.Equal(copy, pending);
        Assert.IsType<MResult.Success>(MembershipWorkflow.Confirm(pending, 190));
    }
}
