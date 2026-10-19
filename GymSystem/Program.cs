using GymSystem;

// Console boundary: reads the file, calls the pure core, prints the result.
string path = args.Length > 0 ? args[0] : Path.Combine(AppContext.BaseDirectory, "data", "clients.csv");

if (!File.Exists(path))
{
    Console.WriteLine($"Data file not found: {path}");
    return 1;
}

string[] lines = File.ReadAllLines(path);
ParsedDataset dataset = ClientParser.ParseLines(lines.Skip(1), firstLineNumber: 2); // line 1 is the header

Console.WriteLine($"Valid clients: {dataset.Clients.Length}, rejected lines: {dataset.Errors.Length}");
foreach (string error in dataset.Errors)
    Console.WriteLine($"  REJECTED {error}");

// --- Assignment 5 part: pipeline with two policies ---
PrintReport("Standard policy", QuotePipeline.Run(dataset.Clients, PricingPolicies.Standard));
PrintReport("Student policy", QuotePipeline.Run(dataset.Clients, PricingPolicies.Student));

// --- Assignment 6 part: safe membership workflow ---
Console.WriteLine();
Console.WriteLine("=== Membership workflow ===");

// 1. Successful operation: valid client -> Pending -> Confirmed
MembershipState? state = null;
Console.WriteLine("1) Valid client Anna:");
state = Apply(state, MembershipWorkflow.Start(
    Client.Create("Anna", 30, MembershipTier.Standard, 3, 2), PricingPolicies.Standard));
state = Apply(state, MembershipWorkflow.Confirm(state, 190));

// 2. Invalid case: the factory rejects the client
Console.WriteLine("2) Invalid client (empty name, age 0):");
Apply(null, MembershipWorkflow.Start(
    Client.Create("", 0, MembershipTier.Basic, 3, 0), PricingPolicies.Standard));

// 3. Invalid case: missing prerequisite (client is valid but under the minimum age)
Console.WriteLine("3) Client under minimum age:");
Apply(null, MembershipWorkflow.Start(
    Client.Create("Boris", 15, MembershipTier.Basic, 6, 0), PricingPolicies.Standard));

// 4. Invalid sequence: confirm without a membership
Console.WriteLine("4) Confirm without a membership:");
Apply(null, MembershipWorkflow.Confirm(null, 190));

// 5. Wrong payment: the Pending state stays unchanged
Console.WriteLine("5) Wrong payment for Chen:");
MembershipState? chen = Apply(null, MembershipWorkflow.Start(
    Client.Create("Chen", 16, MembershipTier.Basic, 1, 0), PricingPolicies.Standard));
chen = Apply(chen, MembershipWorkflow.Confirm(chen, 29));   // rejected, chen is still Pending
Console.WriteLine($"   state after failure: {MembershipWorkflow.Describe(chen)}");

// 6. Repeated operation: second confirmation is rejected, state stays Confirmed
Console.WriteLine("6) Repeated confirmation for Anna:");
state = Apply(state, MembershipWorkflow.Confirm(state, 190));
return 0;

// Prints the outcome. The state is replaced ONLY when the transition succeeded.
static MembershipState? Apply(MembershipState? current, TransitionOutcome outcome)
{
    switch (outcome)
    {
        case TransitionOutcome.Moved moved:
            Console.WriteLine($"   OK: {MembershipWorkflow.Describe(moved.NewState)}");
            return moved.NewState;
        case TransitionOutcome.Rejected rejected:
            Console.WriteLine($"   REJECTED: {rejected.Reason}");
            return current;
        default:
            Console.WriteLine("   REJECTED: unknown outcome");
            return current;
    }
}

static void PrintReport(string title, QuoteReport report)
{
    Console.WriteLine();
    Console.WriteLine($"=== {title} ===");
    Console.WriteLine($"{"Client",-8}{"Tier",-10}{"Months",7}{"Gross",8}{"Discount",10}{"Total",8}");
    foreach (Quote q in report.Quotes)
        Console.WriteLine($"{q.ClientName,-8}{q.Tier,-10}{q.Months,7}{q.GrossCost,8}{q.Discount,10}{q.TotalCost,8}");
    Console.WriteLine($"Total revenue: {report.TotalRevenue}");
}
