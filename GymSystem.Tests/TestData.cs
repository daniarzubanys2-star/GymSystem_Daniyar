using GymSystem;

namespace GymSystem.Tests;

public static class TestData
{
    public static Client Make(string name, int age, MembershipTier tier, int months, int sessions)
        => new Client(name, age, tier, months, sessions);

    // Fresh list on every call, so tests can never influence each other.
    public static List<Client> Sample() => new()
    {
        Make("Anna", 30, MembershipTier.Standard, 3, 2),
        Make("Boris", 15, MembershipTier.Basic, 6, 0),    // too young -> not eligible
        Make("Chen", 16, MembershipTier.Basic, 1, 0),     // boundary age, 0 sessions
        Make("Dana", 22, MembershipTier.Premium, 12, 4),  // boundary months (discount)
        Make("Erik", 45, MembershipTier.Basic, 12, 1),
        Make("Fiona", 25, MembershipTier.Standard, 2, 0), // boundary age for Student policy
    };
}
