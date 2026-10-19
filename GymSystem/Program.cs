using System.Collections.Immutable;
using GymSystem;

// Console boundary. All business decisions are made by the pure core, here we only print.
string dataDir = Path.Combine(AppContext.BaseDirectory, "data");
string path = args.Length > 0 ? args[0] : Path.Combine(dataDir, "clients.csv");

Console.WriteLine("=== 1. Loading data ===");
var loaded = ClientFileLoader.Load(path);
switch (loaded)
{
    case Result<ImmutableArray<Client>, AppError>.Failure failure:
        PrintError(failure.Error);
        return 1;
    case Result<ImmutableArray<Client>, AppError>.Success success:
        RunDemo(success.Value, dataDir);
        return 0;
    default:
        Console.WriteLine("Unknown result.");
        return 1;
}

static void RunDemo(ImmutableArray<Client> clients, string dataDir)
{
    Console.WriteLine($"Loaded {clients.Length} clients.");

    // Assignment 5: pipeline with two policies
    PrintReport("Standard policy", QuotePipeline.Run(clients, PricingPolicies.Standard));
    PrintReport("Student policy", QuotePipeline.Run(clients, PricingPolicies.Student));

    // Assignment 6 + 7: Result chain (Maybe -> Result -> Bind -> Bind -> Map)
    Console.WriteLine();
    Console.WriteLine("=== 2. Enrollment chain ===");
    Console.WriteLine("Normal path (Anna pays 190):");
    PrintResult(Enrollment.EnrollByName(clients, "Anna", PricingPolicies.Standard, 190));
    Console.WriteLine("Domain failure 1 - client not found (Zed):");
    PrintResult(Enrollment.EnrollByName(clients, "Zed", PricingPolicies.Standard, 100));
    Console.WriteLine("Domain failure 2 - not eligible (Boris, age 15):");
    PrintResult(Enrollment.EnrollByName(clients, "Boris", PricingPolicies.Standard, 100));
    Console.WriteLine("Domain failure 3 - wrong payment (Chen pays 29, total is 30):");
    PrintResult(Enrollment.EnrollByName(clients, "Chen", PricingPolicies.Standard, 29));
    Console.WriteLine("Domain failure 4 - invalid state (confirm twice):");
    var first = MembershipWorkflow.Start(clients.First(c => c.Name == "Chen"), PricingPolicies.Standard)
        .Bind(s => MembershipWorkflow.Confirm(s, 30));
    PrintResult(first.Map(s => MembershipWorkflow.Describe(s)));
    PrintResult(first.Bind(s => MembershipWorkflow.Confirm(s, 30)).Map(s => MembershipWorkflow.Describe(s)));

    // Maybe lookups
    Console.WriteLine();
    Console.WriteLine("=== 3. Lookups ===");
    Console.WriteLine("Chen sessions: " + Describe(ClientLookup.FindTrainerSessions(clients, "Chen")));
    Console.WriteLine("Zed sessions:  " + Describe(ClientLookup.FindTrainerSessions(clients, "Zed")));

    // File failures
    Console.WriteLine();
    Console.WriteLine("=== 4. File failures ===");
    PrintLoad(ClientFileLoader.Load(Path.Combine(dataDir, "does_not_exist.csv")));
    PrintLoad(ClientFileLoader.Load(Path.Combine(dataDir, "clients_malformed.csv")));
}

static string Describe(Maybe<int> maybe) => maybe switch
{
    Maybe<int>.Some some => $"found, {some.Value}",
    Maybe<int>.None => "client not found",
    _ => "unknown"
};

static void PrintLoad(Result<ImmutableArray<Client>, AppError> result)
{
    switch (result)
    {
        case Result<ImmutableArray<Client>, AppError>.Success s:
            Console.WriteLine($"   OK: {s.Value.Length} clients");
            break;
        case Result<ImmutableArray<Client>, AppError>.Failure f:
            PrintError(f.Error);
            break;
    }
}

static void PrintResult(Result<string, AppError> result)
{
    switch (result)
    {
        case Result<string, AppError>.Success success:
            Console.WriteLine($"   OK: {success.Value}");
            break;
        case Result<string, AppError>.Failure failure:
            PrintError(failure.Error);
            break;
    }
}

static void PrintError(AppError error)
    => Console.WriteLine($"   FAILED [{error.Kind}/{error.Code}]: {error.Message}");

static void PrintReport(string title, QuoteReport report)
{
    Console.WriteLine();
    Console.WriteLine($"=== {title} ===");
    Console.WriteLine($"{"Client",-8}{"Tier",-10}{"Months",7}{"Gross",8}{"Discount",10}{"Total",8}");
    foreach (Quote q in report.Quotes)
        Console.WriteLine($"{q.ClientName,-8}{q.Tier,-10}{q.Months,7}{q.GrossCost,8}{q.Discount,10}{q.TotalCost,8}");
    Console.WriteLine($"Total revenue: {report.TotalRevenue}");
}
