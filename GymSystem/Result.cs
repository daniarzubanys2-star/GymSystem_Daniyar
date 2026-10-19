namespace GymSystem;

// Result<T, E>: either Success(value) or Failure(error). Nothing else can exist
// (private base constructor) and a Success can never be created with a missing value.
public abstract record Result<T, E>
{
    private Result() { }

    public sealed record Success(T Value) : Result<T, E>;

    public sealed record Failure(E Error) : Result<T, E>;

    public static Result<T, E> Ok(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new Success(value);
    }

    public static Result<T, E> Fail(E error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new Failure(error);
    }
}

public static class ResultExtensions
{
    // Map: Result<T,E> + (T -> U) -> Result<U,E>
    // The function is pure and returns a plain value. On failure it is NOT called.
    public static Result<U, E> Map<T, U, E>(this Result<T, E> result, Func<T, U> transform)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(transform);

        return result switch
        {
            Result<T, E>.Success success => Result<U, E>.Ok(transform(success.Value)),
            Result<T, E>.Failure failure => Result<U, E>.Fail(failure.Error),
            _ => throw new InvalidOperationException("Unknown Result case.")
        };
    }

    // Bind: Result<T,E> + (T -> Result<U,E>) -> Result<U,E>
    // The function itself can fail, so it returns a Result. On failure it is NOT called.
    public static Result<U, E> Bind<T, U, E>(this Result<T, E> result, Func<T, Result<U, E>> next)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(next);

        return result switch
        {
            Result<T, E>.Success success => next(success.Value),
            Result<T, E>.Failure failure => Result<U, E>.Fail(failure.Error),
            _ => throw new InvalidOperationException("Unknown Result case.")
        };
    }
}
