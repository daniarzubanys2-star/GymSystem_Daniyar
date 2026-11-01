using System.Collections.Immutable;
using GymSystem;
using Xunit;

namespace GymSystem.Tests;

public class ReportsAndExtensionsTests
{
    private static ImmutableArray<Quote> SampleQuotes()
        => QuotePipeline.Run(TestData.Sample(), PricingPolicies.Standard).Quotes;
    // Standard totals: Dana 936 (Premium), Erik 342 (Basic), Anna 190 (Standard), Fiona 100 (Standard), Chen 30 (Basic)

    private static Quote Q(string name, MembershipTier tier, int total)
        => new Quote(name, tier, 1, total, 0, total);

    // ---------- REPORT 1 ----------
    [Fact]
    public void HighValue_NormalResult_FilteredAndOrdered_ExactLimitIncluded()
    {
        var rows = Reports.HighValueClients(SampleQuotes(), 100);

        Assert.Equal(new[] { "Dana", "Erik", "Anna", "Fiona" }, rows.Select(r => r.ClientName));   // Fiona = exactly 100
        Assert.Equal(new[] { 936, 342, 190, 100 }, rows.Select(r => r.TotalCost));
    }

    [Fact]
    public void HighValue_EmptyInput_AndNoMatches_GiveEmptyReport()
    {
        Assert.True(Reports.HighValueClients(new List<Quote>(), 100).IsEmpty);
        Assert.True(Reports.HighValueClients(SampleQuotes(), 100000).IsEmpty);
    }

    [Fact]
    public void HighValue_TiedTotals_AreOrderedByNameDeterministically()
    {
        var quotes = new[] { Q("Zoe", MembershipTier.Basic, 200), Q("Adam", MembershipTier.Basic, 200), Q("Mia", MembershipTier.Basic, 200) };

        var rows = Reports.HighValueClients(quotes, 0);

        Assert.Equal(new[] { "Adam", "Mia", "Zoe" }, rows.Select(r => r.ClientName));
    }

    // ---------- REPORT 2 ----------
    [Fact]
    public void RevenueByTier_GroupsAndTotalsCorrectly_AndOrdersByRevenue()
    {
        var summary = Reports.RevenueByTier(SampleQuotes());

        Assert.Equal(new[] { MembershipTier.Premium, MembershipTier.Basic, MembershipTier.Standard },
                     summary.Select(s => s.Tier));
        Assert.Equal(new[] { 936, 372, 290 }, summary.Select(s => s.Revenue));
        Assert.Equal(new[] { 1, 2, 2 }, summary.Select(s => s.Clients));
        Assert.Equal(new[] { 936, 342, 190 }, summary.Select(s => s.HighestTotal));
    }

    [Fact]
    public void RevenueByTier_EmptyInput_GivesEmptyReport_AndTiedRevenueUsesTierOrder()
    {
        Assert.True(Reports.RevenueByTier(new List<Quote>()).IsEmpty);

        var tied = Reports.RevenueByTier(new[] { Q("A", MembershipTier.Premium, 50), Q("B", MembershipTier.Basic, 50) });
        Assert.Equal(new[] { MembershipTier.Basic, MembershipTier.Premium }, tied.Select(s => s.Tier));
    }

    // ---------- EXTENSIONS ----------
    [Fact]
    public void WhereEligible_KeepsAge16_DropsAge15_AndDoesNotChangeSource()
    {
        List<Client> source = TestData.Sample();
        int before = source.Count;

        var eligible = source.WhereEligible().ToList();

        Assert.DoesNotContain(eligible, c => c.Name == "Boris");
        Assert.Contains(eligible, c => c.Name == "Chen");      // exactly 16
        Assert.Equal(before, source.Count);
    }

    [Fact]
    public void OrderForReport_HighestFirst_NameBreaksTies()
    {
        var ordered = new[] { Q("B", MembershipTier.Basic, 10), Q("A", MembershipTier.Basic, 10), Q("C", MembershipTier.Basic, 99) }
            .OrderForReport().Select(q => q.ClientName);

        Assert.Equal(new[] { "C", "A", "B" }, ordered);
    }

    [Fact]
    public void CollectAll_AllSuccess_GivesValues_FirstFailureStopsEvaluation()
    {
        var ok = new[] { 1, 2, 3 }.Select(i => Result<int, AppError>.Ok(i)).CollectAll();
        Assert.Equal(new[] { 1, 2, 3 }, Assert.IsType<Result<ImmutableArray<int>, AppError>.Success>(ok).Value.ToArray());

        int evaluated = 0;
        var error = AppError.InvalidInput("bad");
        var failed = new[] { 1, 2, 3 }.Select(i =>
        {
            evaluated++;
            return i == 2 ? Result<int, AppError>.Fail(error) : Result<int, AppError>.Ok(i);
        }).CollectAll();

        Assert.Equal(error, Assert.IsType<Result<ImmutableArray<int>, AppError>.Failure>(failed).Error);
        Assert.Equal(2, evaluated);   // item 3 was never evaluated

        var empty = new List<Result<int, AppError>>().CollectAll();
        Assert.True(Assert.IsType<Result<ImmutableArray<int>, AppError>.Success>(empty).Value.IsEmpty);
    }

    // ---------- CONFIGURED FUNCTIONS ----------
    [Fact]
    public void CurriedScore_TwoPartialApplications_GiveDifferentResultsOnSameInput()
    {
        var anna = TestData.Make("Anna", 30, MembershipTier.Standard, 3, 2);
        var erik = TestData.Make("Erik", 45, MembershipTier.Basic, 12, 1);
        var balanced = LoyaltyScoring.Balanced();
        var focused = LoyaltyScoring.TrainerFocused();

        Assert.Equal(12, balanced(anna));    // 3*2 + 2*3
        Assert.Equal(27, balanced(erik));    // 12*2 + 1*3
        Assert.Equal(23, focused(anna));     // 3*1 + 2*10
        Assert.Equal(22, focused(erik));     // 12*1 + 1*10

        Assert.True(balanced(erik) > balanced(anna));   // documented different result:
        Assert.True(focused(anna) > focused(erik));     // the ranking of Anna and Erik flips
        Assert.Equal(balanced(anna), LoyaltyScoring.Score(2)(3)(anna));   // same as fully curried call
    }
}
