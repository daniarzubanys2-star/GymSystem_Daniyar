using GymSystem;
using Xunit;

namespace GymSystem.Tests;

public class ClientFactoryTests
{
    // FACTORY SUCCESS
    [Fact]
    public void Factory_ValidInput_CreatesClientWithExpectedData()
    {
        ClientValidation result = Client.Create("  Anna ", 30, MembershipTier.Standard, 3, 2);

        var valid = Assert.IsType<ClientValidation.Valid>(result);
        Assert.Equal("Anna", valid.Client.Name);          // name is trimmed
        Assert.Equal(30, valid.Client.Age);
        Assert.Equal(MembershipTier.Standard, valid.Client.Tier);
        Assert.Equal(3, valid.Client.Months);
        Assert.Equal(2, valid.Client.TrainerSessions);
    }

    // FACTORY REJECTION: every rule rejects and reports its own reason
    [Theory]
    [InlineData("", 30, 1, 3, 0, "Name")]
    [InlineData("  ", 30, 1, 3, 0, "Name")]
    [InlineData("Anna", 0, 1, 3, 0, "Age")]
    [InlineData("Anna", 121, 1, 3, 0, "Age")]
    [InlineData("Anna", 30, 99, 3, 0, "Tier")]
    [InlineData("Anna", 30, 1, 0, 0, "Months")]
    [InlineData("Anna", 30, 1, 37, 0, "Months")]
    [InlineData("Anna", 30, 1, 3, -1, "TrainerSessions")]
    [InlineData("Anna", 30, 1, 3, 1001, "TrainerSessions")]
    public void Factory_InvalidInput_IsRejectedWithReason(
        string name, int age, int tier, int months, int sessions, string expectedReason)
    {
        ClientValidation result = Client.Create(name, age, (MembershipTier)tier, months, sessions);

        var invalid = Assert.IsType<ClientValidation.Invalid>(result);
        Assert.Single(invalid.Reasons);
        Assert.Contains(expectedReason, invalid.Reasons[0]);
    }

    [Fact]
    public void Factory_NullName_IsRejected_AndAllReasonsAreReported()
    {
        ClientValidation result = Client.Create(null, 0, MembershipTier.Basic, 3, 0);

        var invalid = Assert.IsType<ClientValidation.Invalid>(result);
        Assert.Equal(2, invalid.Reasons.Length);   // name and age
    }

    // BOUNDARY VALUES of the factory
    [Fact]
    public void Factory_ExactLimits_AreAccepted()
    {
        Assert.IsType<ClientValidation.Valid>(Client.Create("A", 1, MembershipTier.Basic, 1, 0));
        Assert.IsType<ClientValidation.Valid>(Client.Create("A", 120, MembershipTier.Premium, 36, 0));
    }

    // DISTINCT CASES (parser output): Valid and Invalid, and the 0 sessions value is not "missing"
    [Fact]
    public void Parse_ReturnsValidCase_ForGoodLine_AndZeroSessionsIsKept()
    {
        var valid = Assert.IsType<ClientValidation.Valid>(ClientParser.Parse("Chen,16,Basic,1,0"));
        Assert.Equal(0, valid.Client.TrainerSessions);
    }

    [Theory]
    [InlineData("Gina,abc,Basic,3,0")]
    [InlineData("Hank,30,Gold,3,0")]
    [InlineData("Kim,30,7,3,0")]
    [InlineData("Ivan,30,Basic,0,0")]
    [InlineData("only,three,fields")]
    [InlineData("")]
    [InlineData(null)]
    public void Parse_ReturnsInvalidCase_ForBadLine(string? line)
    {
        var invalid = Assert.IsType<ClientValidation.Invalid>(ClientParser.Parse(line));
        Assert.NotEmpty(invalid.Reasons);
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
}
