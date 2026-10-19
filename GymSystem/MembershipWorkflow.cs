using MembershipResult = GymSystem.Result<GymSystem.MembershipState, GymSystem.AppError>;

namespace GymSystem;

// Safe sequence: Client.Create -> Start -> Confirm. Pure methods, immutable states.
// Expected failures are returned as Result failures, no exceptions are used for business rules.
public static class MembershipWorkflow
{
    // Needs an already validated Client (the factory result). Fails with not_eligible for age < 16.
    public static MembershipResult Start(Client client, Func<Client, int> monthlyFeePolicy)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(monthlyFeePolicy);

        QuoteReport report = QuotePipeline.Run(new[] { client }, monthlyFeePolicy);

        if (report.Quotes.IsEmpty)
            return MembershipResult.Fail(AppError.NotEligible(client.Name));

        return MembershipResult.Ok(new MembershipState.Pending(report.Quotes[0]));
    }

    // Only a Pending membership can be confirmed and the payment must be exact.
    // Repeated confirmation: rejected with invalid_state, the Confirmed state stays unchanged.
    public static MembershipResult Confirm(MembershipState? state, int payment)
    {
        return state switch
        {
            MembershipState.Pending pending when payment == pending.Quote.TotalCost
                => MembershipResult.Ok(new MembershipState.Confirmed(pending.Quote, payment)),

            MembershipState.Pending pending
                => MembershipResult.Fail(AppError.PaymentMismatch(payment, pending.Quote.TotalCost)),

            MembershipState.Confirmed
                => MembershipResult.Fail(AppError.InvalidState("Membership is already confirmed.")),

            _ => MembershipResult.Fail(AppError.InvalidState("There is no membership to confirm."))
        };
    }

    public static string Describe(MembershipState? state) => state switch
    {
        MembershipState.Pending p => $"Pending: {p.Quote.ClientName} must pay {p.Quote.TotalCost}",
        MembershipState.Confirmed c => $"Confirmed: {c.Quote.ClientName} paid {c.PaidAmount}",
        _ => "No membership"
    };
}
