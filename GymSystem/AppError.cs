namespace GymSystem;

// Domain      = an expected business failure (wrong input, unknown client, wrong state).
// Operational = a problem with the environment (missing file, unreadable file, native library).
public enum ErrorKind { Domain, Operational }

// The error type used in every Result<T, AppError>. A Code identifies the rule, the Message explains it.
public sealed record AppError(ErrorKind Kind, string Code, string Message)
{
    // --- domain failures ---
    public static AppError InvalidClient(IEnumerable<string> reasons)
        => new(ErrorKind.Domain, "invalid_client", "Client data is invalid: " + string.Join(" ", reasons));

    public static AppError NotFound(string name)
        => new(ErrorKind.Domain, "client_not_found", $"No client named '{name}' was found.");

    public static AppError NotEligible(string name)
        => new(ErrorKind.Domain, "not_eligible", $"{name} is not eligible: minimum age is {Rules.MinimumAge}.");

    public static AppError PaymentMismatch(int paid, int expected)
        => new(ErrorKind.Domain, "payment_mismatch", $"Payment {paid} does not match the quoted total {expected}.");

    public static AppError InvalidState(string message)
        => new(ErrorKind.Domain, "invalid_state", message);

    public static AppError InvalidInput(string details)
        => new(ErrorKind.Domain, "invalid_input", details);

    // --- operational failures ---
    public static AppError FileNotFound(string path)
        => new(ErrorKind.Operational, "file_not_found", $"Data file not found: {path}");

    public static AppError FileUnreadable(string path, string details)
        => new(ErrorKind.Operational, "file_unreadable", $"Cannot read file {path}: {details}");

    public static AppError FileMalformed(string path, string details)
        => new(ErrorKind.Operational, "file_malformed", $"Malformed content in {path}: {details}");

    public static AppError NativeUnavailable(string details)
        => new(ErrorKind.Operational, "native_unavailable", $"Native library call failed: {details}");

    public static AppError NativeRejected(int months, int sessions)
        => new(ErrorKind.Domain, "native_rejected", $"Native function rejected months={months}, sessions={sessions}.");

    public static AppError ExportFailed(string path, string details)
        => new(ErrorKind.Operational, "export_failed", $"Cannot write {path}: {details}");
}
