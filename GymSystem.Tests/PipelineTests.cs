using GymSystem;
using Xunit;

namespace GymSystem.Tests;

public class PipelineTests
{
    // NORMAL WORKFLOW
    [Fact]
    public void NormalWorkflow_StandardPolicy_GivesDocumentedResult()
    {
        QuoteReport report = QuotePipeline.Run(TestData.Sample(), PricingPolicies.Standard);

        Assert.Equal(new[] { "Dana", "Erik", "Anna", "Fiona", "Chen" },
                     report.Quotes.Select(q => q.ClientName));
        Assert.Equal(new[] { 936, 342, 190, 100, 30 }, report.Quotes.Select(q => q.TotalCost));
        Assert.Equal(1598, report.TotalRevenue);
    }

    // EMPTY DATASET
    [Fact]
    public void EmptyDataset_GivesEmptyReport()
    {
        QuoteReport report = QuotePipeline.Run(new List<Client>(), PricingPolicies.Standard);

        Assert.Empty(report.Quotes);
        Assert.Equal(0, report.TotalRevenue);

        ParsedDataset parsed = ClientParser.ParseLines(new List<string>());
        Assert.Empty(parsed.Clients);
        Assert.Empty(parsed.Errors);
    }

    // BOUNDARY VALUE
    [Fact]
    public void Boundary_Age16IsEligible_Age15IsNot()
    {
        var clients = new List<Client>
        {
            TestData.Make("Young", 15, MembershipTier.Basic, 1, 0),
            TestData.Make("Exact", 16, MembershipTier.Basic, 1, 0),
        };

        var eligible = QuotePipeline.SelectEligible(clients);

        Assert.Single(eligible);
        Assert.Equal("Exact", eligible[0].Name);
    }

    [Fact]
    public void Boundary_DiscountStartsAtTwelveMonths()
    {
        var eleven = QuotePipeline.PriceClients(
            new[] { TestData.Make("A", 30, MembershipTier.Basic, 11, 0) }, PricingPolicies.Standard);
        var twelve = QuotePipeline.PriceClients(
            new[] { TestData.Make("B", 30, MembershipTier.Basic, 12, 0) }, PricingPolicies.Standard);

        Assert.Equal(0, eleven[0].Discount);     // 11 * 30 = 330, no discount
        Assert.Equal(36, twelve[0].Discount);    // 12 * 30 = 360, 10% = 36
    }

    // INVALID AND MISSING INPUT
    [Theory]
    [InlineData("Gina,abc,Basic,3,0")]     // age is not a number
    [InlineData("Hank,30,Gold,3,0")]       // unknown tier
    [InlineData("Kim,30,7,3,0")]           // numeric tier that does not exist
    [InlineData("Ivan,30,Basic,0,0")]      // months below minimum
    [InlineData("Jo,30,Basic,3,-1")]       // negative sessions
    [InlineData("only,three,fields")]      // wrong number of fields
    [InlineData(",30,Basic,3,0")]          // empty name
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void InvalidOrMissingLine_IsRejectedWithMessage(string? line)
    {
        bool ok = ClientParser.TryParse(line, out Client? client, out string error);

        Assert.False(ok);
        Assert.Null(client);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public void ZeroSessionsIsValid_NotTreatedAsMissing()
    {
        bool ok = ClientParser.TryParse("Chen,16,Basic,1,0", out Client? client, out _);

        Assert.True(ok);
        Assert.NotNull(client);
        Assert.Equal(0, client.TrainerSessions);
    }

    [Fact]
    public void NullInput_IsRejected_NoFabricatedResult()
    {
        Assert.Throws<ArgumentNullException>(() => ClientParser.ParseLines(null!));
        Assert.Throws<ArgumentNullException>(() => QuotePipeline.Run(null!, PricingPolicies.Standard));
        Assert.Throws<ArgumentNullException>(() => QuotePipeline.Run(TestData.Sample(), null!));
    }

    [Fact]
    public void ParseLines_SeparatesValidClientsFromErrors()
    {
        string[] lines = { "Anna,30,Standard,3,2", "Gina,abc,Basic,3,0", "Hank,30,Gold,3,0" };

        ParsedDataset result = ClientParser.ParseLines(lines, firstLineNumber: 2);

        Assert.Single(result.Clients);
        Assert.Equal(2, result.Errors.Length);
        Assert.StartsWith("Line 3:", result.Errors[0]);
    }

    // TWO POLICIES
    [Fact]
    public void TwoPolicies_SameInput_DifferInDocumentedCases()
    {
        QuoteReport standard = QuotePipeline.Run(TestData.Sample(), PricingPolicies.Standard);
        QuoteReport student = QuotePipeline.Run(TestData.Sample(), PricingPolicies.Student);

        Assert.Equal(1598, standard.TotalRevenue);
        Assert.Equal(1400, student.TotalRevenue);

        // Fiona is exactly 25 -> student discount applies: 2 * 40 = 80 instead of 100
        Assert.Equal(100, standard.Quotes.Single(q => q.ClientName == "Fiona").TotalCost);
        Assert.Equal(80, student.Quotes.Single(q => q.ClientName == "Fiona").TotalCost);

        // Erik is 45 -> both policies give the same price
        Assert.Equal(342, standard.Quotes.Single(q => q.ClientName == "Erik").TotalCost);
        Assert.Equal(342, student.Quotes.Single(q => q.ClientName == "Erik").TotalCost);
    }

    [Fact]
    public void HigherOrderFunction_ActuallyCallsThePolicy_OncePerEligibleClient()
    {
        int calls = 0;
        Func<Client, int> countingPolicy = c => { calls++; return 10; };

        QuotePipeline.PriceClients(QuotePipeline.SelectEligible(TestData.Sample()), countingPolicy);

        Assert.Equal(5, calls);   // Boris is not eligible, so 5 of 6 clients
    }

    // PURITY AND STAGES
    [Fact]
    public void Stage2_PriceClients_ComputesDocumentedValues()
    {
        var dana = TestData.Make("Dana", 22, MembershipTier.Premium, 12, 4);

        var priced = QuotePipeline.PriceClients(new[] { dana }, PricingPolicies.Standard);

        Assert.Equal(80, priced[0].MonthlyFee);
        Assert.Equal(1040, priced[0].GrossCost);
        Assert.Equal(104, priced[0].Discount);
    }

    [Fact]
    public void Stages_DoNotChangeCallerData_AndGiveSameResultTwice()
    {
        List<Client> input = TestData.Sample();
        List<Client> copyBefore = new(input);

        QuoteReport first = QuotePipeline.Run(input, PricingPolicies.Standard);
        QuoteReport second = QuotePipeline.Run(input, PricingPolicies.Standard);

        Assert.Equal(copyBefore, input);                 // same count, same items, same order
        Assert.True(first.Quotes.SequenceEqual(second.Quotes));   // same input + policy -> same output
        Assert.Equal(first.TotalRevenue, second.TotalRevenue);
    }
}
