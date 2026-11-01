using System.Collections.Immutable;

namespace GymSystem;

// The functional core: every method is pure.
public static class QuotePipeline
{
    // Stage 1: only clients with Age >= 16 (uses the extension method WhereEligible).
    public static ImmutableArray<Client> SelectEligible(IEnumerable<Client> clients)
    {
        ArgumentNullException.ThrowIfNull(clients);
        return clients.WhereEligible().ToImmutableArray();
    }

    // Stage 2 (higher-order): the monthly fee comes from the delegate.
    public static ImmutableArray<PricedClient> PriceClients(
        IEnumerable<Client> clients, Func<Client, int> monthlyFeePolicy)
    {
        ArgumentNullException.ThrowIfNull(clients);
        ArgumentNullException.ThrowIfNull(monthlyFeePolicy);
        return clients.Select(c => PriceClient(c, monthlyFeePolicy)).ToImmutableArray();
    }

    public static PricedClient PriceClient(Client client, Func<Client, int> monthlyFeePolicy)
    {
        int monthlyFee = monthlyFeePolicy(client);   // delegate invocation
        int gross = monthlyFee * client.Months + client.TrainerSessions * Rules.TrainerSessionPrice;
        int discount = client.Months >= Rules.LongTermMonths
            ? gross * Rules.LongTermDiscountPercent / 100
            : 0;
        return new PricedClient(client, monthlyFee, gross, discount);
    }

    public static Quote ToQuote(PricedClient p)
        => new Quote(p.Client.Name, p.Client.Tier, p.Client.Months, p.GrossCost, p.Discount, p.GrossCost - p.Discount);

    // Stage 3 (uses the extension method OrderForReport).
    public static QuoteReport BuildReport(IEnumerable<PricedClient> priced)
    {
        ArgumentNullException.ThrowIfNull(priced);

        ImmutableArray<Quote> quotes = priced.Select(p => ToQuote(p)).OrderForReport().ToImmutableArray();
        return new QuoteReport(quotes, quotes.Sum(q => q.TotalCost));
    }

    public static QuoteReport Run(IEnumerable<Client> clients, Func<Client, int> monthlyFeePolicy)
    {
        var eligible = SelectEligible(clients);                      // stage 1
        var priced = PriceClients(eligible, monthlyFeePolicy);       // stage 2
        return BuildReport(priced);                                  // stage 3
    }
}
