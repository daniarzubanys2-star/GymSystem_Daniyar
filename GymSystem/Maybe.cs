namespace GymSystem;

// Maybe<T>: a value that may be absent. Some(value) is a real value (even 0 or false),
// None means "not found". We never use null or default(T) to represent absence.
public abstract record Maybe<T>
{
    private Maybe() { }

    public sealed record Some(T Value) : Maybe<T>;

    public sealed record None : Maybe<T>;

    public static Maybe<T> Just(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new Some(value);
    }

    public static Maybe<T> Nothing() => new None();
}

public static class MaybeExtensions
{
    // Converts a missing value into a meaningful failure (for required items).
    public static Result<T, E> ToResult<T, E>(this Maybe<T> maybe, E errorIfMissing)
    {
        ArgumentNullException.ThrowIfNull(maybe);

        return maybe switch
        {
            Maybe<T>.Some some => Result<T, E>.Ok(some.Value),
            Maybe<T>.None => Result<T, E>.Fail(errorIfMissing),
            _ => throw new InvalidOperationException("Unknown Maybe case.")
        };
    }
}
