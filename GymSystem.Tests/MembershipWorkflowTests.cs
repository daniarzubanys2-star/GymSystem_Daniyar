using GymSystem;
using Xunit;

namespace GymSystem.Tests;

public class MembershipWorkflowTests
{
    private static ClientValidation AnnaValidation()
        => Client.Create("Anna", 30, MembershipTier.Standard, 3, 2);   // quote total = 190

    private static MembershipState.Pending StartAnna()
    {
        var moved = Assert.IsType<TransitionOutcome.Moved>(
            MembershipWorkflow.Start(AnnaValidation(), PricingPolicies.Standard));
        return Assert.IsType<MembershipState.Pending>(moved.NewState);
    }

    // NORMAL OPERATION: validated value -> pipeline -> Pending -> Confirmed
    [Fact]
    public void NormalOperation_ValidClient_BecomesPending_ThenConfirmed()
    {
        MembershipState.Pending pending = StartAnna();
        Assert.Equal("Anna", pending.Quote.ClientName);
        Assert.Equal(190, pending.Quote.TotalCost);

        var moved = Assert.IsType<TransitionOutcome.Moved>(MembershipWorkflow.Confirm(pending, 190));
        var confirmed = Assert.IsType<MembershipState.Confirmed>(moved.NewState);
        Assert.Equal(190, confirmed.PaidAmount);
    }

    // DISTINCT CASES: both state cases and both outcome cases are reachable and handled
    [Fact]
    public void DistinctCases_PatternMatchingGivesCaseSpecificResult()
    {
        MembershipState.Pending pending = StartAnna();
        var confirmed = (MembershipState.Confirmed)((TransitionOutcome.Moved)MembershipWorkflow.Confirm(pending, 190)).NewState;

        Assert.Equal("Pending: Anna must pay 190", MembershipWorkflow.Describe(pending));
        Assert.Equal("Confirmed: Anna paid 190", MembershipWorkflow.Describe(confirmed));
        Assert.Equal("No membership", MembershipWorkflow.Describe(null));
    }

    // MISSING PREREQUISITE
    [Fact]
    public void MissingPrerequisite_InvalidClient_IsRejectedWithReasons()
    {
        ClientValidation bad = Client.Create("", 0, MembershipTier.Basic, 3, 0);

        var rejected = Assert.IsType<TransitionOutcome.Rejected>(
            MembershipWorkflow.Start(bad, PricingPolicies.Standard));
        Assert.Contains("Name", rejected.Reason);
        Assert.Contains("Age", rejected.Reason);
    }

    [Fact]
    public void MissingPrerequisite_UnderMinimumAge_IsRejected()
    {
        ClientValidation boris = Client.Create("Boris", 15, MembershipTier.Basic, 6, 0);

        var rejected = Assert.IsType<TransitionOutcome.Rejected>(
            MembershipWorkflow.Start(boris, PricingPolicies.Standard));
        Assert.Contains("not eligible", rejected.Reason);
    }

    [Fact]
    public void MissingPrerequisite_ConfirmWithoutMembership_IsRejected()
    {
        Assert.IsType<TransitionOutcome.Rejected>(MembershipWorkflow.Confirm(null, 190));
        Assert.IsType<TransitionOutcome.Rejected>(MembershipWorkflow.Start(null!, PricingPolicies.Standard));
    }

    // BOUNDARY VALUE: the exact total is accepted, one less or one more is rejected
    [Theory]
    [InlineData(189, false)]
    [InlineData(190, true)]
    [InlineData(191, false)]
    public void Boundary_PaymentMustEqualQuotedTotal(int payment, bool accepted)
    {
        TransitionOutcome outcome = MembershipWorkflow.Confirm(StartAnna(), payment);

        if (accepted)
            Assert.IsType<TransitionOutcome.Moved>(outcome);
        else
            Assert.IsType<TransitionOutcome.Rejected>(outcome);
    }

    // REPEATED OPERATION: documented rule = second confirmation is rejected, state is unchanged
    [Fact]
    public void RepeatedConfirmation_IsRejected_AndConfirmedStateStaysTheSame()
    {
        var first = (TransitionOutcome.Moved)MembershipWorkflow.Confirm(StartAnna(), 190);
        MembershipState confirmed = first.NewState;

        var second = Assert.IsType<TransitionOutcome.Rejected>(MembershipWorkflow.Confirm(confirmed, 190));

        Assert.Contains("already confirmed", second.Reason);
        Assert.Equal(190, Assert.IsType<MembershipState.Confirmed>(confirmed).PaidAmount);
    }

    // STATE PRESERVATION: a failed transition leaves the original state untouched and usable
    [Fact]
    public void FailedTransition_LeavesOriginalStateUnchanged()
    {
        MembershipState.Pending pending = StartAnna();
        MembershipState.Pending copy = StartAnna();   // an equal, independent value

        Assert.IsType<TransitionOutcome.Rejected>(MembershipWorkflow.Confirm(pending, 1));

        Assert.Equal(copy, pending);                                   // value did not change
        Assert.IsType<TransitionOutcome.Moved>(MembershipWorkflow.Confirm(pending, 190)); // still usable
    }

    [Fact]
    public void FailedConstruction_ReturnsInvalid_AndDoesNotAffectLaterValidCreation()
    {
        Assert.IsType<ClientValidation.Invalid>(Client.Create("", 0, MembershipTier.Basic, 0, -1));
        Assert.IsType<ClientValidation.Valid>(Client.Create("Anna", 30, MembershipTier.Standard, 3, 2));
    }
}
