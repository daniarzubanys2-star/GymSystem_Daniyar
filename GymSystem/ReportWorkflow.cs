using System.Collections.Immutable;

namespace GymSystem;

public sealed record ReportBundle(
    QuoteReport Quotes,
    ImmutableArray<HighValueRow> HighValue,
    ImmutableArray<TierSummary> ByTier,
    ImmutableArray<LoyaltyRow> Loyalty);

public static class ReportWorkflow
{
    // Loads the file, then builds everything. Failures stay explicit Results.
    public static Result<ReportBundle, AppError> Run(
        string? path, Func<Client, int> pricingPolicy, Func<Client, int> scoreFunction, int minTotal)
        => Run(path, pricingPolicy, scoreFunction, minTotal, NativeBonus.Calculate);

    public static Result<ReportBundle, AppError> Run(
        string? path, Func<Client, int> pricingPolicy, Func<Client, int> scoreFunction, int minTotal,
        Func<int, int, Result<int, AppError>> bonusFunction)
        => ClientFileLoader.Load(path)
            .Bind(clients => Build(clients, pricingPolicy, scoreFunction, minTotal, bonusFunction));

    // PURE part (no file, no Console). The configured functions are passed in as arguments:
    //   pricingPolicy (A5), scoreFunction (currying), bonusFunction (native adapter).
    public static Result<ReportBundle, AppError> Build(
        IEnumerable<Client> clients, Func<Client, int> pricingPolicy, Func<Client, int> scoreFunction,
        int minTotal, Func<int, int, Result<int, AppError>> bonusFunction)
    {
        ArgumentNullException.ThrowIfNull(clients);
        ArgumentNullException.ThrowIfNull(pricingPolicy);
        ArgumentNullException.ThrowIfNull(scoreFunction);
        ArgumentNullException.ThrowIfNull(bonusFunction);

        // Snapshot: the eligible clients are materialised once and reused below.
        ImmutableArray<Client> eligible = clients.WhereEligible().ToImmutableArray();
        QuoteReport quotes = QuotePipeline.Run(eligible, pricingPolicy);

        return eligible
            .Select(c => bonusFunction(c.Months, c.TrainerSessions)               // may fail (native call)
                .Map(bonus => new LoyaltyRow(c.Name, scoreFunction(c), bonus)))
            .CollectAll()
            .Map(rows => new ReportBundle(
                quotes,
                Reports.HighValueClients(quotes.Quotes, minTotal),
                Reports.RevenueByTier(quotes.Quotes),
                rows.OrderByDescending(r => r.Score)
                    .ThenBy(r => r.ClientName, StringComparer.Ordinal)
                    .ToImmutableArray()));
    }
}
