using System.Collections.Immutable;

namespace GymSystem;

// Discriminated-union style model: the result of trying to create a Client.
//   Valid   - carries the validated Client (and nothing else)
//   Invalid - carries the list of reasons (and no Client)
// The base constructor is private, so only these two nested cases can exist.
public abstract record ClientValidation
{
    private ClientValidation() { }

    public sealed record Valid(Client Client) : ClientValidation;

    public sealed record Invalid(ImmutableArray<string> Reasons) : ClientValidation;
}
