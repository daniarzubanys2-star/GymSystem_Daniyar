using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace GymSystem;

// Application boundary: raw text -> valid Client. Nothing here is silently replaced by a default.
public static class ClientParser
{
    // Line format: Name,Age,Tier,Months,TrainerSessions
    public static bool TryParse(string? line, [NotNullWhen(true)] out Client? client, out string error)
    {
        client = null;

        if (string.IsNullOrWhiteSpace(line))
        {
            error = "Line is missing or empty.";
            return false;
        }

        string[] parts = line.Split(',');
        if (parts.Length != 5)
        {
            error = $"Expected 5 fields but found {parts.Length}.";
            return false;
        }

        string name = parts[0].Trim();
        if (name.Length == 0)
        {
            error = "Name must not be empty.";
            return false;
        }

        if (!int.TryParse(parts[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int age)
            || age < 1 || age > Rules.MaxAge)
        {
            error = $"Age must be a whole number between 1 and {Rules.MaxAge}.";
            return false;
        }

        // Enum.TryParse also accepts "7", so IsDefined is needed as well.
        if (!Enum.TryParse(parts[2].Trim(), true, out MembershipTier tier) || !Enum.IsDefined(tier))
        {
            error = "Tier must be Basic, Standard or Premium.";
            return false;
        }

        if (!int.TryParse(parts[3].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int months)
            || months < 1 || months > Rules.MaxMonths)
        {
            error = $"Months must be a whole number between 1 and {Rules.MaxMonths}.";
            return false;
        }

        // 0 sessions is a legitimate value, only negative numbers are rejected.
        if (!int.TryParse(parts[4].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int sessions)
            || sessions < 0)
        {
            error = "TrainerSessions must be a whole number, 0 or more.";
            return false;
        }

        client = new Client(name, age, tier, months, sessions);
        error = "";
        return true;
    }

    public static ParsedDataset ParseLines(IEnumerable<string?> lines, int firstLineNumber = 1)
    {
        ArgumentNullException.ThrowIfNull(lines);

        var clients = ImmutableArray.CreateBuilder<Client>();
        var errors = ImmutableArray.CreateBuilder<string>();
        int lineNumber = firstLineNumber;

        foreach (string? line in lines)
        {
            if (TryParse(line, out Client? client, out string error))
                clients.Add(client);
            else
                errors.Add($"Line {lineNumber}: {error}");
            lineNumber++;
        }

        return new ParsedDataset(clients.ToImmutable(), errors.ToImmutable());
    }
}
