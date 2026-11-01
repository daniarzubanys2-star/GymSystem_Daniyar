using System.Collections.Immutable;

namespace GymSystem;

// A Client that exists is always valid:
//  - the constructor is private, so nobody can write "new Client(...)" with unchecked data;
//  - the properties are get-only, so a value can not be changed afterwards
//    (not even with a "with" expression).
// The only way to get a Client is Client.Create.
public sealed record Client
{
    public string Name { get; }
    public int Age { get; }
    public MembershipTier Tier { get; }
    public int Months { get; }
    public int TrainerSessions { get; }

    private Client(string name, int age, MembershipTier tier, int months, int trainerSessions)
    {
        Name = name;
        Age = age;
        Tier = tier;
        Months = months;
        TrainerSessions = trainerSessions;
    }

    // Validated factory: returns Valid(client) or Invalid(all reasons). Never a half-built object.
    public static ClientValidation Create(string? name, int age, MembershipTier tier, int months, int trainerSessions)
    {
        string cleanName = (name ?? "").Trim();
        var reasons = new List<string>();

        if (cleanName.Length == 0)
            reasons.Add("Name must not be empty.");
        if (age < 1 || age > Rules.MaxAge)
            reasons.Add($"Age must be between 1 and {Rules.MaxAge}.");
        if (!Enum.IsDefined(tier))
            reasons.Add("Tier must be Basic, Standard or Premium.");
        if (months < 1 || months > Rules.MaxMonths)
            reasons.Add($"Months must be between 1 and {Rules.MaxMonths}.");
        if (trainerSessions < 0 || trainerSessions > Rules.MaxTrainerSessions)
            reasons.Add($"TrainerSessions must be between 0 and {Rules.MaxTrainerSessions}.");

        if (reasons.Count > 0)
            return new ClientValidation.Invalid(reasons.ToImmutableArray());

        return new ClientValidation.Valid(new Client(cleanName, age, tier, months, trainerSessions));
    }
}
