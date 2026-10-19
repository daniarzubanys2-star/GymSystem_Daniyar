namespace GymSystem;

public static class Enrollment
{
    // The complete Result chain:
    //   Maybe<Client> --ToResult--> Result<Client>          (missing client -> client_not_found)
    //   --Bind(Start)-->   Result<MembershipState>          (may fail: not_eligible)
    //   --Bind(Confirm)--> Result<MembershipState>          (may fail: payment_mismatch / invalid_state)
    //   --Map(Describe)--> Result<string>                   (pure, can not fail)
    // Bind is used when the next step can fail (returns a Result); Map when it returns a plain value.
    public static Result<string, AppError> EnrollByName(
        IEnumerable<Client> clients, string? name, Func<Client, int> monthlyFeePolicy, int payment)
    {
        ArgumentNullException.ThrowIfNull(clients);

        return ClientLookup.FindByName(clients, name)
            .ToResult(AppError.NotFound(name ?? ""))
            .Bind(client => MembershipWorkflow.Start(client, monthlyFeePolicy))
            .Bind(state => MembershipWorkflow.Confirm(state, payment))
            .Map(state => MembershipWorkflow.Describe(state));
    }
}
