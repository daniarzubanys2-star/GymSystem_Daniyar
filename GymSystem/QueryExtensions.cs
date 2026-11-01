using System.Collections.Immutable;

namespace GymSystem;

// Custom extension methods. Both are used in the application (see QuotePipeline and ReportWorkflow).
public static class QueryExtensions
{
    // Receiver: IEnumerable<Client>. Returns the clients allowed to get a quote (age >= 16).
    // It belongs on the client sequence because "eligible" is a rule about clients and is
    // reused by the pipeline, the lazy quotes and the report workflow. Does not change the source.
    public static IEnumerable<Client> WhereEligible(this IEnumerable<Client> clients)
        => clients.Where(c => c.Age >= Rules.MinimumAge);

    // Receiver: IEnumerable<Quote>. Standard report order: highest total first, name as tie-breaker.
    public static IOrderedEnumerable<Quote> OrderForReport(this IEnumerable<Quote> quotes)
        => quotes.OrderByDescending(q => q.TotalCost)
                 .ThenBy(q => q.ClientName, StringComparer.Ordinal);

    // Receiver: IEnumerable<Result<T,E>>. Combines many results into one:
    // all Success -> Success(list of values); the FIRST Failure -> that Failure.
    // It stops at the first failure, so later items of a lazy sequence are never evaluated.
    public static Result<ImmutableArray<T>, E> CollectAll<T, E>(this IEnumerable<Result<T, E>> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        var values = ImmutableArray.CreateBuilder<T>();
        foreach (Result<T, E> result in results)
        {
            switch (result)
            {
                case Result<T, E>.Success success:
                    values.Add(success.Value);
                    break;
                case Result<T, E>.Failure failure:
                    return Result<ImmutableArray<T>, E>.Fail(failure.Error);
                default:
                    throw new InvalidOperationException("Unknown Result case.");
            }
        }
        return Result<ImmutableArray<T>, E>.Ok(values.ToImmutable());
    }
}
