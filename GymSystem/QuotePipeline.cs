using System.Collections.Immutable;

namespace GymSystem;

// The functional core: every method is pure (no Console, no files, no global state,
// the input collections are never modified).
public static class QuotePipeline
{
    // Stage 1: IEnumerable<Client> -> ImmutableArray<Client>
    // Rule: only clients with Age >= MinimumAge are eligible (age 16 is accepted).
    // Example: [Boris(15), Chen(16)] -> [Chen]
    public static ImmutableArray<Client> SelectEligible(IEnumerable<Client> clients)
    {
        ArgumentNullException.ThrowIfNull(clients);
        return clients.Where(c => c.Age >= Rules.MinimumAge).ToImmutableArray();
    }

    // Stage 2: (clients, Func<Client,int>) -> ImmutableArray<PricedClient>
    // HIGHER-ORDER FUNCTION: the monthly fee comes from the delegate monthlyFeePolicy.
    // gross = fee * months + sessions * 20; discount = 10% of gross when months >= 12.
    // Example (Standard policy): Dana(Premium, 12 months, 4 sessions) -> fee 80, gross 1040, discount 104
    public static ImmutableArray<PricedClient> PriceClients(
        IEnumerable<Client> clients, Func<Client, int> monthlyFeePolicy)
    {
        ArgumentNullException.ThrowIfNull(clients);
        ArgumentNullException.ThrowIfNull(monthlyFeePolicy);
        return clients.Select(c => Price(c, monthlyFeePolicy)).ToImmutableArray();
    }

    private static PricedClient Price(Client client, Func<Client, int> monthlyFeePolicy)
    {
        int monthlyFee = monthlyFeePolicy(client);   // delegate invocation
        int gross = monthlyFee * client.Months + client.TrainerSessions * Rules.TrainerSessionPrice;
        int discount = client.Months >= Rules.LongTermMonths
            ? gross * Rules.LongTermDiscountPercent / 100
            : 0;
        return new PricedClient(client, monthlyFee, gross, discount);
    }

    // Stage 3: IEnumerable<PricedClient> -> QuoteReport
    // Builds the quotes, sorts them (highest total first, name as tie-breaker) and sums the revenue.
    public static QuoteReport BuildReport(IEnumerable<PricedClient> priced)
    {
        ArgumentNullException.ThrowIfNull(priced);

        ImmutableArray<Quote> quotes = priced
            .Select(p => new Quote(p.Client.Name, p.Client.Tier, p.Client.Months,
                                   p.GrossCost, p.Discount, p.GrossCost - p.Discount))
            .OrderByDescending(q => q.TotalCost)
            .ThenBy(q => q.ClientName, StringComparer.Ordinal)
            .ToImmutableArray();

        return new QuoteReport(quotes, quotes.Sum(q => q.TotalCost));
    }

    // The visible workflow: the three stages are connected by ordinary function calls.
    public static QuoteReport Run(IEnumerable<Client> clients, Func<Client, int> monthlyFeePolicy)
    {
        var eligible = SelectEligible(clients);                      // stage 1
        var priced = PriceClients(eligible, monthlyFeePolicy);       // stage 2
        return BuildReport(priced);                                  // stage 3
    }
}
