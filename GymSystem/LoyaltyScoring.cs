namespace GymSystem;

// Currying and partial application.
// Score takes its arguments one by one: monthWeight -> sessionWeight -> client -> score.
// Fixing the first two arguments gives a configured function Client -> int.
public static class LoyaltyScoring
{
    public static readonly Func<int, Func<int, Func<Client, int>>> Score =
        monthWeight => sessionWeight => client =>
            client.Months * monthWeight + client.TrainerSessions * sessionWeight;

    // Partial applications (two configurations of the same function):
    public static Func<Client, int> Balanced() => Score(2)(3);          // months count 2, sessions count 3
    public static Func<Client, int> TrainerFocused() => Score(1)(10);   // sessions count much more
}

public sealed record LoyaltyRow(string ClientName, int Score, int BonusPoints);
