using System.Collections.Immutable;
using GymSystem;

// Console boundary: file access, printing and the native library are used here and in the
// adapters. The reports and calculations themselves are pure.
string dataDir = Path.Combine(AppContext.BaseDirectory, "data");
string path = args.Length > 0 ? args[0] : Path.Combine(dataDir, "clients.csv");
string outDir = Path.Combine(AppContext.BaseDirectory, "out");

Console.WriteLine("=== 1. Load data ===");
var loaded = ClientFileLoader.Load(path);   // lazy file reader inside, materialised snapshot here
if (loaded is not Result<ImmutableArray<Client>, AppError>.Success loadedOk)
{
    PrintError(((Result<ImmutableArray<Client>, AppError>.Failure)loaded).Error);
    return 1;
}
ImmutableArray<Client> clients = loadedOk.Value;
Console.WriteLine($"Loaded {clients.Length} clients from {path}");

// --- Assignment 8 workflow: two LINQ reports + loyalty score with the native bonus ---
Console.WriteLine();
Console.WriteLine("=== 2. Reports (Standard pricing, balanced score) ===");
var normal = ReportWorkflow.Build(clients, PricingPolicies.Standard, LoyaltyScoring.Balanced(), 100, NativeBonus.Calculate);
switch (normal)
{
    case Result<ReportBundle, AppError>.Success success:
        PrintBundle(success.Value);
        Export(success.Value, outDir);
        break;
    case Result<ReportBundle, AppError>.Failure failure:
        PrintError(failure.Error);
        break;
}

// --- Currying: two partially applied functions on the same data ---
Console.WriteLine();
Console.WriteLine("=== 3. Currying: two configured score functions ===");
PrintLoyalty("Balanced (months x2, sessions x3)", clients, LoyaltyScoring.Balanced());
PrintLoyalty("TrainerFocused (months x1, sessions x10)", clients, LoyaltyScoring.TrainerFocused());

// --- Deferred execution ---
Console.WriteLine();
Console.WriteLine("=== 4. Deferred execution ===");
Func<Client, int> tracingPolicy = c => { Console.WriteLine($"   pricing {c.Name}"); return PricingPolicies.Standard(c); };
IEnumerable<Quote> lazy = LazyQuotes.Generate(clients, tracingPolicy);
Console.WriteLine("Enumerable created, nothing priced yet. Taking the first quote only:");
Quote first = lazy.First();
Console.WriteLine($"First: {first.ClientName} {first.TotalCost}");
Console.WriteLine("Now ToArray() (snapshot):");
Quote[] snapshot = lazy.ToArray();
Console.WriteLine($"Snapshot has {snapshot.Length} quotes; using it again prices nothing.");

// --- Failure paths ---
Console.WriteLine();
Console.WriteLine("=== 5. Failures ===");
Console.WriteLine("Native library not available (simulated by an injected function):");
PrintResultOnly(ReportWorkflow.Build(clients, PricingPolicies.Standard, LoyaltyScoring.Balanced(), 100,
    (m, s) => NativeBonus.Calculate(m, s, (_, _) => throw new DllNotFoundException("gymnative"))));
Console.WriteLine("Missing data file:");
PrintResultOnly(ReportWorkflow.Run(Path.Combine(dataDir, "does_not_exist.csv"),
    PricingPolicies.Standard, LoyaltyScoring.Balanced(), 100));
Console.WriteLine("Malformed data file:");
PrintResultOnly(ReportWorkflow.Run(Path.Combine(dataDir, "clients_malformed.csv"),
    PricingPolicies.Standard, LoyaltyScoring.Balanced(), 100));
return 0;

static void PrintBundle(ReportBundle bundle)
{
    Console.WriteLine($"Quotes: {bundle.Quotes.Quotes.Length}, revenue {bundle.Quotes.TotalRevenue}");
    Console.WriteLine();
    Console.WriteLine("Report 1 - high-value clients (total >= 100):");
    foreach (HighValueRow r in bundle.HighValue)
        Console.WriteLine($"   {r.ClientName,-8}{r.Tier,-10}{r.TotalCost,6}");
    Console.WriteLine("Report 2 - revenue by tier:");
    foreach (TierSummary t in bundle.ByTier)
        Console.WriteLine($"   {t.Tier,-10} clients {t.Clients}  revenue {t.Revenue,5}  highest {t.HighestTotal,5}");
    Console.WriteLine("Loyalty (score + native bonus points):");
    foreach (LoyaltyRow l in bundle.Loyalty)
        Console.WriteLine($"   {l.ClientName,-8} score {l.Score,3}  bonus {l.BonusPoints,4}");
}

static void Export(ReportBundle bundle, string outDir)
{
    Directory.CreateDirectory(outDir);
    string target = Path.Combine(outDir, "revenue_by_tier.csv");
    var result = ReportExporter.WriteLines(target, ReportFormatter.TierSummaryLines(bundle.ByTier));
    if (result is Result<string, AppError>.Success ok)
        Console.WriteLine($"Report 2 exported to {ok.Value}");
    else if (result is Result<string, AppError>.Failure failure)
        PrintError(failure.Error);
}

static void PrintLoyalty(string title, IEnumerable<Client> clients, Func<Client, int> score)
{
    Console.WriteLine(title);
    var ranking = clients.WhereEligible()
        .Select(c => (c.Name, Score: score(c)))
        .OrderByDescending(x => x.Score)
        .ThenBy(x => x.Name, StringComparer.Ordinal);
    foreach (var x in ranking)
        Console.WriteLine($"   {x.Name,-8}{x.Score,4}");
}

static void PrintResultOnly<T>(Result<T, AppError> result)
{
    if (result is Result<T, AppError>.Failure failure)
        PrintError(failure.Error);
    else
        Console.WriteLine("   OK");
}

static void PrintError(AppError error)
    => Console.WriteLine($"   FAILED [{error.Kind}/{error.Code}]: {error.Message}");
