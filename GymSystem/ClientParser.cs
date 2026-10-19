using System.Collections.Immutable;
using System.Globalization;

namespace GymSystem;

// Application boundary: raw text -> ClientValidation (Valid or Invalid).
// The text is converted to numbers here; the RULES are checked by Client.Create.
public static class ClientParser
{
    // Line format: Name,Age,Tier,Months,TrainerSessions
    public static ClientValidation Parse(string? line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return Invalid("Line is missing or empty.");

        string[] parts = line.Split(',');
        if (parts.Length != 5)
            return Invalid($"Expected 5 fields but found {parts.Length}.");

        var reasons = new List<string>();

        if (!int.TryParse(parts[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int age))
            reasons.Add("Age must be a whole number.");
        if (!Enum.TryParse(parts[2].Trim(), true, out MembershipTier tier))
            reasons.Add("Tier must be Basic, Standard or Premium.");
        if (!int.TryParse(parts[3].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int months))
            reasons.Add("Months must be a whole number.");
        if (!int.TryParse(parts[4].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int sessions))
            reasons.Add("TrainerSessions must be a whole number.");

        if (reasons.Count > 0)
            return new ClientValidation.Invalid(reasons.ToImmutableArray());

        return Client.Create(parts[0], age, tier, months, sessions);   // the factory checks the rules
    }

    public static ParsedDataset ParseLines(IEnumerable<string?> lines, int firstLineNumber = 1)
    {
        ArgumentNullException.ThrowIfNull(lines);

        var clients = ImmutableArray.CreateBuilder<Client>();
        var errors = ImmutableArray.CreateBuilder<string>();
        int lineNumber = firstLineNumber;

        foreach (string? line in lines)
        {
            switch (Parse(line))
            {
                case ClientValidation.Valid valid:
                    clients.Add(valid.Client);
                    break;
                case ClientValidation.Invalid invalid:
                    errors.Add($"Line {lineNumber}: {string.Join(" ", invalid.Reasons)}");
                    break;
                default:
                    errors.Add($"Line {lineNumber}: unknown validation result.");
                    break;
            }
            lineNumber++;
        }

        return new ParsedDataset(clients.ToImmutable(), errors.ToImmutable());
    }

    private static ClientValidation Invalid(string reason)
        => new ClientValidation.Invalid(ImmutableArray.Create(reason));
}
