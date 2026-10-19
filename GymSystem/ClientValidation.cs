using System.Collections.Immutable;

namespace GymSystem;

// Meaning: the outcome of CHECKING raw data for a client (Valid / Invalid with all reasons).
//   Valid   - carries the validated Client
//   Invalid - carries the list of reasons
// (Result<T,E> is used for operations of the workflow; ClientValidation.ToResult connects the two.)
public abstract record ClientValidation
{
    private ClientValidation() { }

    public sealed record Valid(Client Client) : ClientValidation;

    public sealed record Invalid(ImmutableArray<string> Reasons) : ClientValidation;
}

public static class ClientValidationExtensions
{
    public static Result<Client, AppError> ToResult(this ClientValidation validation)
    {
        return validation switch
        {
            ClientValidation.Valid valid => Result<Client, AppError>.Ok(valid.Client),
            ClientValidation.Invalid invalid => Result<Client, AppError>.Fail(AppError.InvalidClient(invalid.Reasons)),
            _ => Result<Client, AppError>.Fail(AppError.InvalidClient(new[] { "Missing validation result." }))
        };
    }
}
