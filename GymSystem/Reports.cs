using System.Collections.Immutable;

namespace GymSystem;

public sealed record HighValueRow(string ClientName, MembershipTier Tier, int TotalCost);
public sealed record TierSummary(MembershipTier Tier, int Clients, int Revenue, int HighestTotal);

// Two LINQ reports. They are pure: input is a sequence of quotes, output is an immutable array.
// No Console and no file access here.
public static class Reports
{
    // REPORT 1 - "High-value clients" (user: gym manager who wants to call the best customers).
    // Input: quotes, minimum total. Filter: TotalCost >= minTotal (exact limit included).
    // Output: ClientName, Tier, TotalCost; highest total first, name as tie-breaker.
    // Stages: Where -> OrderByDescending -> ThenBy -> Select.
    public static ImmutableArray<HighValueRow> HighValueClients(IEnumerable<Quote> quotes, int minTotal)
    {
        ArgumentNullException.ThrowIfNull(quotes);

        return quotes
            .Where(q => q.TotalCost >= minTotal)
            .OrderByDescending(q => q.TotalCost)
            .ThenBy(q => q.ClientName, StringComparer.Ordinal)
            .Select(q => new HighValueRow(q.ClientName, q.Tier, q.TotalCost))
            .ToImmutableArray();
    }

    // REPORT 2 - "Revenue by tier" (user: accountant).
    // Output per tier: number of clients (Count), revenue (Sum), highest single total (Aggregate).
    // Stages: GroupBy -> Select -> OrderByDescending(revenue) -> ThenBy(tier).
    public static ImmutableArray<TierSummary> RevenueByTier(IEnumerable<Quote> quotes)
    {
        ArgumentNullException.ThrowIfNull(quotes);

        return quotes
            .GroupBy(q => q.Tier)
            .Select(g => new TierSummary(
                g.Key,
                g.Count(),
                g.Sum(q => q.TotalCost),
                g.Aggregate(0, (highest, q) => Math.Max(highest, q.TotalCost))))
            .OrderByDescending(s => s.Revenue)
            .ThenBy(s => s.Tier)
            .ToImmutableArray();
    }
}

// Pure text formatting for export (no file access).
public static class ReportFormatter
{
    public static IEnumerable<string> TierSummaryLines(IEnumerable<TierSummary> summaries)
    {
        yield return "Tier,Clients,Revenue,HighestTotal";
        foreach (TierSummary s in summaries)
            yield return $"{s.Tier},{s.Clients},{s.Revenue},{s.HighestTotal}";
    }
}
