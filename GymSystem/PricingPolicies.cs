namespace GymSystem;

// Two policies for the SAME rule "monthly fee of a client".
// Both have the type Func<Client, int>, so the pipeline can accept either of them.
public static class PricingPolicies
{
    public static int Standard(Client client) => client.Tier switch
    {
        MembershipTier.Basic => 30,
        MembershipTier.Standard => 50,
        MembershipTier.Premium => 80,
        _ => throw new ArgumentOutOfRangeException(nameof(client), "Unknown tier.")
    };

    // Same prices, but clients up to 25 years old get 20% off the monthly fee.
    public static int Student(Client client)
    {
        int fee = Standard(client);
        return client.Age <= Rules.StudentMaxAge
            ? fee * (100 - Rules.StudentDiscountPercent) / 100
            : fee;
    }
}
