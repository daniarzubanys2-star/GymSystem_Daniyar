namespace GymSystem;

// Deferred execution with yield return.
public static class LazyQuotes
{
    // Not an iterator itself, so the argument checks run immediately when Generate is called.
    // (In an iterator method the checks would be postponed until the first MoveNext.)
    public static IEnumerable<Quote> Generate(IEnumerable<Client> clients, Func<Client, int> monthlyFeePolicy)
    {
        ArgumentNullException.ThrowIfNull(clients);
        ArgumentNullException.ThrowIfNull(monthlyFeePolicy);
        return GenerateCore(clients, monthlyFeePolicy);
    }

    // Creating the enumerable runs NONE of this body. Each iteration step prices one client.
    // Every new enumeration starts again and calls the policy again; use ToArray() for a snapshot.
    private static IEnumerable<Quote> GenerateCore(IEnumerable<Client> clients, Func<Client, int> policy)
    {
        foreach (Client client in clients.WhereEligible())
            yield return QuotePipeline.ToQuote(QuotePipeline.PriceClient(client, policy));
    }
}
