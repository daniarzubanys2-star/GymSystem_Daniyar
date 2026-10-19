namespace GymSystem;

// Discriminated-union style model: the state of a membership.
//   Pending   - a quote was calculated, waiting for payment (carries the Quote)
//   Confirmed - the exact payment was received (carries the Quote and the PaidAmount)
// Both constructors are internal: outside this project nobody can create a Confirmed state
// directly. It is only reachable through MembershipWorkflow.Confirm.
public abstract record MembershipState
{
    private MembershipState() { }

    public sealed record Pending : MembershipState
    {
        public Quote Quote { get; }
        internal Pending(Quote quote) { Quote = quote; }
    }

    public sealed record Confirmed : MembershipState
    {
        public Quote Quote { get; }
        public int PaidAmount { get; }
        internal Confirmed(Quote quote, int paidAmount) { Quote = quote; PaidAmount = paidAmount; }
    }
}

// Outcome of a transition: either a new state or a rejection with a reason.
// (In Assignment 7 this will be replaced by a general Result type.)
public abstract record TransitionOutcome
{
    private TransitionOutcome() { }

    public sealed record Moved(MembershipState NewState) : TransitionOutcome;

    public sealed record Rejected(string Reason) : TransitionOutcome;
}
