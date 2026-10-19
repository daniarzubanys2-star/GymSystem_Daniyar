using System.Collections.Immutable;

namespace GymSystem;

public enum MembershipTier { Basic, Standard, Premium }

// Client is now in Client.cs: it can only be created through a validating factory.

// Output of stage 2: the client together with the calculated money values.
public sealed record PricedClient(Client Client, int MonthlyFee, int GrossCost, int Discount);

// One line of the final result.
public sealed record Quote(string ClientName, MembershipTier Tier, int Months, int GrossCost, int Discount, int TotalCost);

// Output of stage 3. ImmutableArray is a real immutable copy, callers cannot change it.
public sealed record QuoteReport(ImmutableArray<Quote> Quotes, int TotalRevenue);

// Result of reading the CSV: the valid clients and the error messages of the bad lines.
public sealed record ParsedDataset(ImmutableArray<Client> Clients, ImmutableArray<string> Errors);
