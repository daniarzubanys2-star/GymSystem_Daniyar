namespace GymSystem;

// Safe sequence: Client.Create -> Start -> Confirm.
// Every step needs the result of the previous one as an explicit argument,
// so the order can not be forgotten. All methods are pure: they return a new state
// and never change the old one.
public static class MembershipWorkflow
{
    // Needs a ClientValidation (the result of the factory) and the pricing policy.
    public static TransitionOutcome Start(ClientValidation validation, Func<Client, int> monthlyFeePolicy)
    {
        ArgumentNullException.ThrowIfNull(monthlyFeePolicy);

        return validation switch
        {
            ClientValidation.Valid valid => StartForValidClient(valid.Client, monthlyFeePolicy),
            ClientValidation.Invalid invalid => new TransitionOutcome.Rejected(
                "Client is invalid: " + string.Join(" ", invalid.Reasons)),
            _ => new TransitionOutcome.Rejected("Missing or unknown validation result.")
        };
    }

    private static TransitionOutcome StartForValidClient(Client client, Func<Client, int> policy)
    {
        // Reuses the pipeline from Assignment 5. A client under the minimum age gives no quote.
        QuoteReport report = QuotePipeline.Run(new[] { client }, policy);

        if (report.Quotes.IsEmpty)
            return new TransitionOutcome.Rejected(
                $"{client.Name} is not eligible: minimum age is {Rules.MinimumAge}.");

        return new TransitionOutcome.Moved(new MembershipState.Pending(report.Quotes[0]));
    }

    // Needs a state. Only a Pending membership can be confirmed, and the payment must be exact.
    // Repeated confirmation rule: it is REJECTED and the existing Confirmed state stays unchanged.
    public static TransitionOutcome Confirm(MembershipState? state, int payment)
    {
        return state switch
        {
            MembershipState.Pending pending when payment == pending.Quote.TotalCost
                => new TransitionOutcome.Moved(new MembershipState.Confirmed(pending.Quote, payment)),

            MembershipState.Pending pending
                => new TransitionOutcome.Rejected(
                    $"Payment {payment} does not match the quoted total {pending.Quote.TotalCost}."),

            MembershipState.Confirmed
                => new TransitionOutcome.Rejected("Membership is already confirmed."),

            _ => new TransitionOutcome.Rejected("There is no membership to confirm.")
        };
    }

    public static string Describe(MembershipState? state) => state switch
    {
        MembershipState.Pending p => $"Pending: {p.Quote.ClientName} must pay {p.Quote.TotalCost}",
        MembershipState.Confirmed c => $"Confirmed: {c.Quote.ClientName} paid {c.PaidAmount}",
        _ => "No membership"
    };
}
