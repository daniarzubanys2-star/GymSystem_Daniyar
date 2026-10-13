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

// The policy is chosen HERE (outside the pure pipeline) and passed as an argument.
PrintReport("Standard policy", QuotePipeline.Run(dataset.Clients, PricingPolicies.Standard));
PrintReport("Student policy", QuotePipeline.Run(dataset.Clients, PricingPolicies.Student));
return 0;

static void PrintReport(string title, QuoteReport report)
{
    Console.WriteLine();
    Console.WriteLine($"=== {title} ===");
    Console.WriteLine($"{"Client",-8}{"Tier",-10}{"Months",7}{"Gross",8}{"Discount",10}{"Total",8}");
    foreach (Quote q in report.Quotes)
        Console.WriteLine($"{q.ClientName,-8}{q.Tier,-10}{q.Months,7}{q.GrossCost,8}{q.Discount,10}{q.TotalCost,8}");
    Console.WriteLine($"Total revenue: {report.TotalRevenue}");
}
