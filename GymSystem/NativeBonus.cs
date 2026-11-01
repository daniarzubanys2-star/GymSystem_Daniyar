using System.Runtime.InteropServices;

namespace GymSystem;

// P/Invoke declaration. "gymnative" is resolved by .NET to gymnative.dll (Windows),
// libgymnative.so (Linux) or libgymnative.dylib (macOS) in the application folder.
internal static class NativeMethods
{
    [DllImport("gymnative", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int gym_bonus_points(int months, int trainerSessions);
}

// Adapter: validates the domain input BEFORE the native call and turns loading/binding
// problems into Result failures. Native calls never happen inside pure query code.
public static class NativeBonus
{
    public static Result<int, AppError> Calculate(int months, int trainerSessions)
        => Calculate(months, trainerSessions, NativeMethods.gym_bonus_points);

    // The native function is a parameter so a test can inject a failing one.
    public static Result<int, AppError> Calculate(int months, int trainerSessions, Func<int, int, int> nativeFunction)
    {
        ArgumentNullException.ThrowIfNull(nativeFunction);

        if (months < 1 || months > Rules.MaxMonths)
            return Result<int, AppError>.Fail(AppError.InvalidInput($"Months must be between 1 and {Rules.MaxMonths}."));
        if (trainerSessions < 0 || trainerSessions > Rules.MaxTrainerSessions)
            return Result<int, AppError>.Fail(AppError.InvalidInput($"TrainerSessions must be between 0 and {Rules.MaxTrainerSessions}."));

        try
        {
            int points = nativeFunction(months, trainerSessions);
            return points < 0
                ? Result<int, AppError>.Fail(AppError.NativeRejected(months, trainerSessions))
                : Result<int, AppError>.Ok(points);
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException)
        {
            return Result<int, AppError>.Fail(AppError.NativeUnavailable($"{ex.GetType().Name}: {ex.Message}"));
        }
    }
}
