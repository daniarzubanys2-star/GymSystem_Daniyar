namespace GymSystem;

// Lookups return Maybe, so "not found" is explicit and never a fake Client.
public static class ClientLookup
{
    public static Maybe<Client> FindByName(IEnumerable<Client> clients, string? name)
    {
        ArgumentNullException.ThrowIfNull(clients);

        if (string.IsNullOrWhiteSpace(name))
            return Maybe<Client>.Nothing();

        foreach (Client client in clients)
        {
            if (string.Equals(client.Name, name.Trim(), StringComparison.OrdinalIgnoreCase))
                return Maybe<Client>.Just(client);
        }
        return Maybe<Client>.Nothing();
    }

    // A client with 0 trainer sessions is FOUND: Some(0). Only an unknown client gives None.
    public static Maybe<int> FindTrainerSessions(IEnumerable<Client> clients, string? name)
    {
        return FindByName(clients, name) switch
        {
            Maybe<Client>.Some some => Maybe<int>.Just(some.Value.TrainerSessions),
            _ => Maybe<int>.Nothing()
        };
    }
}
