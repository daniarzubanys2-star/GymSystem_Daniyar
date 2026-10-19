using GymSystem;

namespace GymSystem.Tests;

public static class TestData
{
    // Clients can only be created through the factory, so the helper uses it
    // and fails the test immediately if the test data itself is invalid.
    public static Client Make(string name, int age, MembershipTier tier, int months, int sessions)
        => Client.Create(name, age, tier, months, sessions) switch
        {
            ClientValidation.Valid v => v.Client,
            ClientValidation.Invalid i => throw new InvalidOperationException(
                "Test data is invalid: " + string.Join("; ", i.Reasons)),
            _ => throw new InvalidOperationException("Unknown validation case.")
        };

    public static List<Client> Sample() => new()
    {
        Make("Anna", 30, MembershipTier.Standard, 3, 2),
        Make("Boris", 15, MembershipTier.Basic, 6, 0),    // valid client, but not eligible (age < 16)
        Make("Chen", 16, MembershipTier.Basic, 1, 0),
        Make("Dana", 22, MembershipTier.Premium, 12, 4),
        Make("Erik", 45, MembershipTier.Basic, 12, 1),
        Make("Fiona", 25, MembershipTier.Standard, 2, 0),
    };
}
